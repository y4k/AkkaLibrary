using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Akka;

namespace AkkaLibrary.DataSynchronisation
{

    public static class ExtractionExpressionBuilder
    {
        private static Queue<string> CreateExtractionQueue(string extractionString)
        {
            return new Queue<string>(extractionString.Split(".", StringSplitOptions.RemoveEmptyEntries));
        }

        public static LambdaExpression BuildExtractionExpression(Type sourceType, string extraction)
        {
            var extractionList = CreateExtractionQueue(extraction);

            var sourceParameter = Expression.Parameter(sourceType);

            var exp = ParseExtraction(sourceParameter, extractionList);

            return Expression.Lambda(exp, sourceParameter);
        }

        private static Expression ParseExtraction(Expression sourceParameter, Queue<string> extractionList)
        {
            if(sourceParameter == null)
            {
                return Expression.Empty();
            }

            var currentExtraction = extractionList.Dequeue();

            var extractionType = DetermineExtractionType(currentExtraction);

            Expression extractedExpression = Expression.Empty();
            var match = extractionType.Match()
                    .With<UnknownExtractionType>(ext => throw new ArgumentException($"Could not parse extraction:{ext.Name}"))
                    .With<PropertyExtractionType>(ext =>
                    {
                        extractedExpression = GetPropertyExtractionExpression(sourceParameter, ext.Name);
                    })
                    .With<EnumerableExtractionType>(ext =>
                    {
                        extractedExpression = GetEnumerablePropertyExtractionExpression(sourceParameter, ext.Name, ext.Index);
                    })
                    .With<KeyEnumerableExtractionType>(ext =>
                    {
                        extractedExpression = GetEnumerablePropertyExtractionExpression(sourceParameter, ext.Name, ext.Key);
                    })
                    .With<KeyValueExtractionType>(ext =>
                    {
                        extractedExpression = GetKeyValuePropertyExtractionExpression(sourceParameter, ext.Name, ext.KeyName, ext.KeyValue);
                    });
            
            if(!match.WasHandled)
            {
                throw new ArgumentException($"Unhandled extraction:{currentExtraction}");
            }

            if(extractionList.Count > 0)
            {
                return ParseExtraction(extractedExpression, extractionList);
            }
            return extractedExpression;
        }

        private static Expression GetKeyValuePropertyExtractionExpression(Expression sourceParameter, string name, string keyName, string keyValue)
        {
            try
            {
                var enumerableExpression = GetPropertyExtractionExpression(sourceParameter, name);

                var enumerableType = enumerableExpression.Type;

                if(!(typeof(IEnumerable).IsAssignableFrom(enumerableType)))
                {
                    throw new ArgumentException($"Extraction {name}|{keyName}|{keyValue} does not represent an enumerable.");
                }

                var indexerName = enumerableExpression.Type.GetDefaultMembers().First()?.Name;
                return Expression.Property(enumerableExpression, indexerName, Expression.Constant(keyValue));

                var whereParam = Expression.Parameter(enumerableType, "e");
                var whereKey = Expression.Constant(keyName, whereParam.Type);
                var whereValue = Expression.Constant(keyValue, whereParam.Type);

                var equality = Expression.Equal(whereParam, whereValue);

                var select = Expression.Call(
                            typeof(Enumerable),
                            "Where",
                            new Type[]
                            {
                                enumerableExpression.Type,
                                enumerableType
                            },
                            enumerableExpression,//The enumerable to iterate over.
                            Expression.Lambda(//The value property on the object to find
                                equality,
                                whereParam)
                            );

                var propertyInfo = sourceParameter.Type.GetProperty(keyName);
                return Expression.MakeIndex(enumerableExpression, propertyInfo, new[]{ Expression.Constant(keyValue)});
            }
            catch(Exception ex)
            {
                throw new ArgumentException($"Key-Value expression is not understood. {keyName} and {keyValue}. {ex.Message}");
            }
        }

        private static Expression GetEnumerablePropertyExtractionExpression(Expression sourceParameter, string name, string key)
        {
            var enumerableExpression = GetPropertyExtractionExpression(sourceParameter, name);
            
            if(typeof(IEnumerable).IsAssignableFrom(enumerableExpression.Type))
            {
                var indexerName = enumerableExpression.Type.GetDefaultMembers().First()?.Name;
                return Expression.Property(enumerableExpression, indexerName, Expression.Constant(key));
            }
            throw new ArgumentException($"Enumerable expression is not an array and does not implement IEnumerable");
        }

        private static Expression GetEnumerablePropertyExtractionExpression(Expression sourceParameter, string name, int index)
        {
            var enumerableExpression = GetPropertyExtractionExpression(sourceParameter, name);

            if(enumerableExpression.Type.IsArray)
            {
                return Expression.ArrayIndex(enumerableExpression, Expression.Constant(index));                
            }
            else if(typeof(IEnumerable).IsAssignableFrom(enumerableExpression.Type))
            {
                var indexerName = enumerableExpression.Type.GetDefaultMembers().First()?.Name;
                return Expression.Property(enumerableExpression, indexerName, Expression.Constant(index));
            }
            throw new ArgumentException($"Enumerable expression is not an array and does not implement IEnumerable");
        }

        private static Expression GetPropertyExtractionExpression(Expression sourceParameter, string propertyName)
        {
            var propertyInfo = sourceParameter.Type.GetProperty(propertyName);

            var getter = propertyInfo.GetGetMethod();

            return Expression.Property(sourceParameter, getter);
        }

        private static IExtractionType DetermineExtractionType(string ext)
        {
            //Incorrectly formatted extraction string.
            if(string.IsNullOrWhiteSpace(ext))
            {
                return new UnknownExtractionType(ext);
            }

            //If the extraction contains a '[' or ']'
            if(ext.Contains("[") || ext.Contains("]"))
            {
                //Extraction must contain ONE pair of square brackets only
                if(ext.Count(x => x == '[') != 1 && ext.Count(x => x == ']') != 1)
                {
                    return new UnknownExtractionType(ext);
                }

                var openIndex = ext.IndexOf('[');
                var closeIndex = ext.IndexOf(']');
                var propertyName = ext.Substring(0,openIndex);

                //If the final character isn't a close bracket or the property name is empty, fail.
                if(!ext.EndsWith(']') || string.IsNullOrEmpty(propertyName))
                {
                    return new UnknownExtractionType(ext);
                }

                //Parse contents of bracket.
                var contents = ext.Substring(openIndex+1, closeIndex - openIndex - 1);
                if (int.TryParse(contents, out int index))
                {
                    return new EnumerableExtractionType(propertyName, index);
                }
                else if(!contents.Contains("|"))
                {
                    return new KeyEnumerableExtractionType(propertyName, contents);
                }

                var kvp = contents.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if(kvp.Length != 2)
                {
                    return new UnknownExtractionType(ext);
                }
                else
                {
                    return new KeyValueExtractionType(propertyName, kvp[0], kvp[1]);
                }
            }

            if (!IdentifierExtensions.IsValidIdentifier(ext))
            {
                return new UnknownExtractionType(ext);
            }

            return new PropertyExtractionType(ext);
        }
    }

    public class KeyEnumerableExtractionType : IExtractionType
    {
        public string Name { get; }

        public string Key { get; }

        public KeyEnumerableExtractionType(string name, string key)
        {
            Name = name;
            Key = key;
        }
    }

    public interface IExtractionType
    {
        string Name { get; }
    }

    public class UnknownExtractionType : IExtractionType
    {
        public string Name { get; }

        public UnknownExtractionType(string name)
        {
            Name = name;
        }
    }

    public class PropertyExtractionType : IExtractionType
    {
        public string Name { get; }

        public PropertyExtractionType(string name)
        {
            Name = name;
        }
    }
    
    public class EnumerableExtractionType : IExtractionType
    {
        public string Name  { get; }
        public int Index { get; }

        public EnumerableExtractionType(string name, int index)
        {
            Name = name;
            Index = index;
        }
    }

    public class KeyValueExtractionType : IExtractionType
    {
        public string Name { get; }
        public string KeyName { get; }
        public string KeyValue { get; }

        public KeyValueExtractionType(string name, string keyName, string keyValue)
        {
            Name = name;
            KeyName = keyName;
            KeyValue = keyValue;
        }
    }

    public enum ExtractionType
    {
        Unknown,
        Property,
        Enumerable,
        KeyValue
    }
}