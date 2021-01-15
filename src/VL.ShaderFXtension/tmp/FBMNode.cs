﻿using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core.Extensions;

namespace VL.ShaderFXtension
{
    public class FBMNode<TIn, TOut> : ShaderNode<TOut>
    {
        private const string ShaderCode = @"
${resultType} fbmRidge${signature}${id}(${argumentType} p, ${referenceSignature}){
    ${resultType} noiseVal = abs(${referenceCall} - 0.5) * 2; 
    noiseVal = 1. - noiseVal;
    noiseVal = pow(noiseVal , 10.);
    return noiseVal;
}

${resultType} fbmTurbulence${signature}${id}(${argumentType} p, ${referenceSignature}){
    ${resultType} noiseVal = abs(${referenceCall} - 0.5) * 2; 
    noiseVal = pow(noiseVal , 1.);
    return noiseVal;
}

${resultType} fbmStandard${signature}${id}(${argumentType} p, ${referenceSignature}){
    return ${referenceCall};
}

${resultType} fbm${signature}${id}(${argumentType} p,float gain, float octaves, float lacunarity, ${referenceSignature})
{

    float myScale = 1;
    float myFallOff = gain;

    int iOctaves = int(floor(octaves)); 
    ${resultType} myResult = 0.;  
    float myAmp = 0.;

    for(int i = 0; i < iOctaves;i++){
        myResult += fbm${fbmType}${signature}${id}(p * myScale,${referenceArguments}) * myFallOff;
        myAmp += myFallOff;
        myFallOff *= gain;
        myScale *= lacunarity;
    }

    float myBlend = octaves - float(iOctaves);
    myResult += fbm${fbmType}${signature}${id}(p * myScale,${referenceArguments}) * myFallOff * myBlend;    
    myAmp += myFallOff * myBlend;
    
    if(myAmp > 0.0){
        myResult /= myAmp;
    }
 
    return myResult;
}";
        private const string ShaderCall = "${function}(${arguments})";

        private readonly GPUReference<TIn> _myReference;
        private readonly FractalType _myType;

        public FBMNode(GPUReference<TIn> theReference, 
            OrderedDictionary<string, AbstractGpuValue> inputs,
            FractalType theType = FractalType.Standard) : base("fbm")
        {
            _myReference = theReference;
            _myType = theType;
            inputs.Add("function", theReference);

            var sourceCode =
                ShaderTemplateEvaluator.Evaluate("${resultType} ${resultName} = fbm${signature}${id}(${arguments});",
                    new Dictionary<string, string>
                    {
                        {"resultName", Output.ID},
                        {"resultType", TypeHelpers.GetNameForType<TOut>().ToLower()},
                        {"signature", $"{TypeHelpers.GetNameForType<TIn>()}To{TypeHelpers.GetNameForType<TOut>()}"},
                        {"id", _myReference.ParentNode.ID},
                        {"arguments", BuildArguments(inputs)}
                    });
            Setup(sourceCode, inputs);
        }

        private string BuildArguments(OrderedDictionary<string, AbstractGpuValue> inputs)
        {
            var stringBuilder = new StringBuilder();
            var replacement = new Dictionary<string, AbstractGpuValue>
                {{_myReference.ArguemntKey, new GPUReferenceOverride("p")}};
            inputs.ForEach(input =>
            {
                if (input.Value == null) return;
                if (input.Value is AbstractGPUReference) return;
                stringBuilder.Append(input.Value.ID);
                stringBuilder.Append(", ");
            });
            var myReferenceArguments = _myReference.ParentNode.ReferenceArguments(replacement);
            if (!myReferenceArguments.IsNullOrEmpty())
            {
                stringBuilder.Append(myReferenceArguments);
            }
            else
            {
                if (stringBuilder.Length > 2) stringBuilder.Remove(stringBuilder.Length - 2, 2);
            }
            return stringBuilder.ToString();
        }

        public override IDictionary<string,string> Functions
        {
            get
            {
                var replacement = new Dictionary<string, AbstractGpuValue> {{_myReference.ArguemntKey, new GPUReferenceOverride("p")}};
                return new Dictionary<string, string>() {
                {
                    "fbm"+Output.ID,
                    ShaderTemplateEvaluator.Evaluate(ShaderCode, new Dictionary<string, string>
                {
                    {"resultName", Output.ID},
                    {"resultType", TypeHelpers.GetNameForType<TOut>().ToLower()},
                    {"argumentType", TypeHelpers.GetNameForType<TIn>().ToLower()},
                    {"signature", $"{TypeHelpers.GetNameForType<TIn>()}To{TypeHelpers.GetNameForType<TOut>()}"},
                    {"id", _myReference.ParentNode.ID},
                    {"referenceCall", _myReference.ParentNode.ReferenceCall(replacement)},
                    {"referenceSignature", _myReference.ParentNode.ReferenceSignature(replacement)},
                    {"referenceArguments", _myReference.ParentNode.ReferenceCallArguments(replacement)},
                    {"fbmType", _myType.ToString()}
                })} };
            }
        }
        
        public string ReferenceCall(Dictionary<string,AbstractGpuValue> theReplacements)
        {
            var myCopy = new Dictionary<string,AbstractGpuValue>(Ins);
            theReplacements.ForEach(kv => myCopy[kv.Key] = kv.Value);
            
            return ShaderTemplateEvaluator.Evaluate(
                ShaderCall,
                new Dictionary<string, string>
                {
                    {"function",_myFunction}, 
                    {"arguments",BuildCallArguments(myCopy)}
                });
        }

        public  string ReferenceSignature(Dictionary<string,AbstractGpuValue> theReplacements)
        {
            var myCopy = new Dictionary<string,AbstractGpuValue>(Ins);
            theReplacements.ForEach(kv => myCopy.Remove(kv.Key));
            
            var stringBuilder = new StringBuilder();
            myCopy.ForEach(input =>
            {
                if (input.Value == null) return;
                if (input.Value is GPUReferenceOverride) return;
                stringBuilder.Append(TypeHelpers.GetType(input.Value).ToLower() + " ");
                stringBuilder.Append(input.Key+Math.Abs(input.GetHashCode()));
                stringBuilder.Append(", ");
            });
            if(stringBuilder.Length > 2)stringBuilder.Remove(stringBuilder.Length - 2, 2);
            return stringBuilder.ToString();
        }
        
        public  string ReferenceCallArguments(Dictionary<string,AbstractGpuValue> theReplacements)
        {
            var myCopy = new Dictionary<string,AbstractGpuValue>(Ins);
            theReplacements.ForEach(kv => myCopy.Remove(kv.Key));
            
            var stringBuilder = new StringBuilder();
            myCopy.ForEach(input =>
            {
                if (input.Value == null) return;
                if (input.Value is GPUReferenceOverride) return;
                stringBuilder.Append(input.Key+Math.Abs(input.GetHashCode()));
                stringBuilder.Append(", ");
            });
            if(stringBuilder.Length > 2)stringBuilder.Remove(stringBuilder.Length - 2, 2);
            return stringBuilder.ToString();
        }
        
        public  string ReferenceArguments(Dictionary<string,AbstractGpuValue> theReplacements)
        {
            var myCopy = new Dictionary<string,AbstractGpuValue>(Ins);
            theReplacements.ForEach(kv => myCopy.Remove(kv.Key));
            
            var stringBuilder = new StringBuilder();
            myCopy.ForEach(input =>
            {
                if (input.Value == null) return;
                if (input.Value is GPUReferenceOverride) return;
                stringBuilder.Append(input.Value.ID);
                stringBuilder.Append(", ");
            });
            if(stringBuilder.Length > 2)stringBuilder.Remove(stringBuilder.Length - 2, 2);
            return stringBuilder.ToString();
        }
    }
}