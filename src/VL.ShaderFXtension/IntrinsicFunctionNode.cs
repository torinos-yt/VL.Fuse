﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stride.Core.Extensions;
using VL.Stride.Shaders.ShaderFX;

namespace VL.ShaderFXtension
{
    public class IntrinsicFunctionNode<T> : ShaderNode<T>
    {
        private const string ShaderCode = "${resultType} ${resultName} = ${function}(${arguments});";
        
        public IntrinsicFunctionNode(OrderedDictionary<string,AbstractGpuValue> inputs, string theFunction, ConstantValue<T> theDefault) : base(theFunction, theDefault)
        {
            
            var hasNullValue = false;
            inputs.ForEach(kv =>
            {
                if (kv.Value == null) hasNullValue = true;
            });
            
            var myCode = ShaderCode;
            if (hasNullValue)
            {
                var myKeyMap = new Dictionary<string, string>
                {
                    {"resultName", Output.ID},
                    {"resultType", TypeHelpers.GetNameForType<T>().ToLower()},
                    {"default", theDefault.ID}
                };
                myCode = ShaderTemplateEvaluator.Evaluate(DefaultShaderCode, myKeyMap);
            }
            else
            {
                myCode = ShaderTemplateEvaluator.Evaluate(ShaderCode, new Dictionary<string, string>
                {
                    {"resultName", Output.ID},
                    {"resultType",TypeHelpers.GetNameForType<T>().ToLower()},
                    {"function",theFunction},
                    {"arguments",ShaderNodesUtil.BuildArguments(inputs)}
                });
            }
            Setup(myCode, inputs);
        }
        
        
    }
}