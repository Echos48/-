using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using ChillAIMod;
using AIChatMod.Utils;

namespace AIChat.Core
{
    public enum ThinkMode { Default, Enable, Disable }

    public struct LLMRequestContext
    {
        public string Prompt { get; set; }
        public string ApiKey { get; set; }
        public string ModelName { get; set; }
        public string Persona { get; set; }
        public string UserContent { get; set; }
        public bool UseOllama { get; set; }
        public bool UseMemory { get; set; }
        public bool LogApiRequestBody { get; set; }
        public ThinkMode ThinkMode { get; set; }
        public HierarchicalMemory HierarchicalMemory { get; set; }
    }

    public static class LLMUtils
    {
        public static string BuildRequestBody(LLMRequestContext requestContext)
        {
            string apiKey = requestContext.ApiKey;
            string modelName = requestContext.ModelName;
            string persona = requestContext.Persona;
            
            // 【集成分层记忆】获取带记忆上下文的提示词
            string promptWithMemory = GetContextWithMemory(requestContext.HierarchicalMemory, requestContext.Prompt);
            
            // 【调试日志】显示完整的请求内容
            Log.Info($"[记忆系统] 启用状态: {requestContext.UseMemory}");
            Log.Info($"[发送给LLM的完整内容]\n========================================\n[System Prompt]\n{persona}\n\n[User Content + Memory]\n{promptWithMemory}\n========================================");

            string jsonBody = "";
            string extraJson = requestContext.UseOllama ? $@",""stream"": false" : "";
            // 【深度思考参数】
            extraJson += GetThinkParameterJson(requestContext.ThinkMode);
            
            if (modelName.Contains("gemma")) {
                // 将 persona 作为背景信息放在 user 消息的最前面
                string finalPrompt = $"[System Instruction]\n{persona}\n\n[User Message]\n{promptWithMemory}";
                jsonBody = $@"{{ ""model"": ""{modelName}"", ""messages"": [ {{ ""role"": ""user"", ""content"": ""{ResponseParser.EscapeJson(finalPrompt)}"" }} ]{extraJson} }}";
            } else {
                // Gemini 或 Ollama (如果是 Llama3 等) 通常支持 system role
                jsonBody = $@"{{ ""model"": ""{modelName}"", ""messages"": [ {{ ""role"": ""system"", ""content"": ""{ResponseParser.EscapeJson(persona)}"" }}, {{ ""role"": ""user"", ""content"": ""{ResponseParser.EscapeJson(promptWithMemory)}"" }} ]{extraJson} }}";
            }
            // string jsonBody = $@"{{ ""model"": ""{modelName}"", ""messages"": [ {{ ""role"": ""system"", ""content"": ""{EscapeJson(persona)}"" }}, {{ ""role"": ""user"", ""content"": ""{EscapeJson(promptWithMemory)}"" }} ]{extraJson} }}";
            
            // 【日志】打印完整的请求体（如果启用）
            if (requestContext.LogApiRequestBody)
            {
                Log.Info($"[API请求] 完整请求体:\n{jsonBody}");
            }

            return jsonBody;
        }

        /// <summary>
        /// 获取深度思考参数的 JSON 字符串
        /// </summary>
        private static string GetThinkParameterJson(ThinkMode thinkMode)
        {
            if (thinkMode == ThinkMode.Enable)
            {
                return @",""think"": true";
            }
            else if (thinkMode == ThinkMode.Disable)
            {
                return @",""think"": false";
            }
            // Default 模式不添加 think 参数
            return "";
        }

        private static string GetContextWithMemory(HierarchicalMemory hierarchicalMemory, string currentPrompt)
        {
            if (hierarchicalMemory != null)
            {
                string memoryContext = hierarchicalMemory.GetContext();
                Log.Info($"[记忆系统] 当前记忆状态:\n{hierarchicalMemory.GetMemoryStats()}");

                // 如果有记忆内容，则拼接；否则只返回当前提示
                if (!string.IsNullOrWhiteSpace(memoryContext))
                {
                    return $"{memoryContext}\n\n【Current Input】\n{currentPrompt}";
                }
            }
            
            // 无记忆或未启用，直接返回原始 prompt
            return currentPrompt;
        }
    }
}
