using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Akka;

namespace AkkaLibrary.DataSynchronisation
{
    public static class AssignmentExpressionBuilder
    {
        private static Queue<string> CreateExtractionQueue(string extractionString)
        {
            return new Queue<string>(extractionString.Split(new[]{"."}, StringSplitOptions.RemoveEmptyEntries));
        }

        public static LambdaExpression BuildAssignmentExpression(Type sourceType, string assignmentMapping)
        {
            var assignmentList = CreateExtractionQueue(assignmentMapping);
            
            //Type from which to SET property
            var sourceParameter = Expression.Parameter(sourceType);

            //Get the nested assignment expression
            var propertyExpression = ParseAssignment(sourceParameter, assignmentList);

            //The type of the parameter that will be assigned to
            var assignedToParameter = Expression.Parameter(propertyExpression.Type);

            //Assign the value to the property
            var assignmentExpression = Expression.Assign(propertyExpression, assignedToParameter);

            return Expression.Lambda(assignmentExpression, sourceParameter, assignedToParameter);
        }

        private static Expression ParseAssignment(Expression sourceParameter, Queue<string> assignmentList)
        {
            if (sourceParameter == null)
            {
                return Expression.Empty();
            }

            var currentAssignment = assignmentList.Dequeue();

            var assignmentType = DetermineAssignmentType(currentAssignment);

            Expression assignmentExpression = Expression.Empty();
            var match = assignmentType.Match()
                        .With<UnknownAssignmentType>(asg => throw new ArgumentException($"Could not parse assignment:{asg.Name}"))
                        .With<PropertyAssignmentType>(asg => assignmentExpression = GetPropertyAssignmentExpression(sourceParameter, asg.Name))
                        .With<EnumerableAssignmentType>(asg => assignmentExpression = GetEnumerableAssignmentExpression(sourceParameter, asg.Name, asg.Index));

            if (!match.WasHandled)
            {
                throw new ArgumentException($"Unhandled assignment:{currentAssignment}");
            }

            if (assignmentList.Count > 0)
            {
                return ParseAssignment(assignmentExpression, assignmentList);
            }

            return assignmentExpression;
        }

        private static IAssignmentType DetermineAssignmentType(string assign)
        {
            if (string.IsNullOrWhiteSpace(assign))
            {
                return new UnknownAssignmentType(assign);
            }

            if (assign.Contains("[") || assign.Contains("]"))
            {
                //Extraction must contain ONE pair of square brackets only
                if (assign.Count(x => x == '[') != 1 && assign.Count(x => x == ']') != 1)
                {
                    return new UnknownAssignmentType(assign);
                }

                var openIndex = assign.IndexOf('[');
                var closeIndex = assign.IndexOf(']');
                var propertyName = assign.Substring(0, openIndex);

                //If the final character isn't a close bracket or the property name is empty, fail.
                if (!assign.EndsWith("]") || string.IsNullOrEmpty(propertyName))
                {
                    return new UnknownAssignmentType(assign);
                }

                //Parse contents of bracket.
                var contents = assign.Substring(openIndex + 1, closeIndex - openIndex - 1);
                if (int.TryParse(contents, out int index))
                {
                    return new EnumerableAssignmentType(propertyName, index);
                }

                var kvp = contents.Split(new[]{"|"}, StringSplitOptions.RemoveEmptyEntries);
                if (kvp.Length != 2)
                {
                    return new UnknownAssignmentType(assign);
                }
                else
                {
                    return new KeyValueAssignmentType(propertyName, kvp[0], kvp[1]);
                }
            }

            if (!IdentifierExtensions.IsValidIdentifier(assign))
            {
                return new UnknownAssignmentType(assign);
            }

            return new PropertyAssignmentType(assign);
        }

        private static Expression GetEnumerableAssignmentExpression(Expression sourceParameter, string name, int index)
        {
            var enumerableExpression = GetPropertyAssignmentExpression(sourceParameter, name);

            if (enumerableExpression.Type.IsArray)
            {
                return Expression.ArrayIndex(enumerableExpression, Expression.Constant(index));
            }
            else if (typeof(IEnumerable).IsAssignableFrom(enumerableExpression.Type))
            {
                var indexerName = enumerableExpression.Type.GetDefaultMembers().First()?.Name;
                return Expression.Property(enumerableExpression, indexerName, Expression.Constant(index));
            }
            throw new ArgumentException($"Enumerable expression is not an array and does not implemenet IEnumerable");
        }

        private static Expression GetPropertyAssignmentExpression(Expression sourceParameter, string propertyName)
        {
            var propertyInfo = sourceParameter.Type.GetProperty(propertyName);

            var setter = propertyInfo.GetSetMethod();

            return Expression.Property(sourceParameter, setter);
        }
    }

    public interface IAssignmentType
    {
        string Name { get; }
    }

    public class UnknownAssignmentType : IAssignmentType
    {
        public string Name { get; }

        public UnknownAssignmentType(string name)
        {
            Name = name;
        }
    }

    public class PropertyAssignmentType : IAssignmentType
    {
        public string Name { get; }
        
        public PropertyAssignmentType(string name)
        {
            Name = name;
        }
    }

    public class EnumerableAssignmentType : IAssignmentType
    {
        public string Name { get; }
        public int Index { get; }
        
        public EnumerableAssignmentType(string name, int index)
        {
            Name = name;
            Index = index;
        }
    }
        
    public class KeyValueAssignmentType : IAssignmentType
    {
        public string Name { get; }
        public string Key { get; }
        public string Value { get; }
        
        public KeyValueAssignmentType(string name, string key, string value)
        {
            Name = name;
            Key = key;
            Value = value;
        }
    }
}