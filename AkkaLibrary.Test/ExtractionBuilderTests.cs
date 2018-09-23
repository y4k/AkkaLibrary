using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AkkaLibrary.DataSynchronisation;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace AkkaLibrary.Test
{
    public class ExtractionBuilderTests
    {
        private readonly Gen<ExtractionTestObject> _objGen;

        public ExtractionBuilderTests()
        {
            _objGen = from d in Gen.Choose(0, int.MaxValue).Select(x => (double)x)
                      from str in Arb.From<NonNull<string>>().Generator
                      from i in Arb.From<int>().Generator
                      from iArray in Arb.From<int[]>().Generator
                      from floats in Arb.From<List<float>>().Generator
                      select new ExtractionTestObject
                      {
                          StringTest = str.Get,
                          DoubleTest = d,
                          IntTest = i,
                          IntArrayTest = iArray,
                          FloatListTest = floats
                      };
        }

        [Property(Skip = "Ignored. Extractors currently unused.")]
        public Property SimpleStringPropertyExtraction()
        {
            return Prop.ForAll<ExtractionTestObject>(Arb.From(_objGen), testObject =>
            {
                const string extractionString = "StringTest";

                var expression = ExtractionExpressionBuilder
                                    .BuildExtractionExpression(
                                        typeof(ExtractionTestObject),
                                        extractionString
                                        );
                var parameter = Expression.Parameter(typeof(ExtractionTestObject));
                var lambda = Expression.Lambda<Func<ExtractionTestObject, string>>(expression, parameter);
                var func = lambda.Compile();

                var result = func(testObject);
            });
        }
    }

    public class ExtractionTestObject
    {
        public string StringTest { get; set; }
        public int IntTest { get; set; }
        public double DoubleTest { get; set; }
        public int[] IntArrayTest { get; set; }
        public List<float> FloatListTest { get; set; }
    }
}