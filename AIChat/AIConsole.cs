using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

using AIChat.Utils;
using AIChat.Core;
using ChillAIMod;

namespace AIChatConsoleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Info("Console version of AIChat started.");
            string memoryFilePath = Path.Combine("ChillAIMod", "memory.txt");

            while (true)
            {
                string prompt = Console.ReadLine();
                var requestContext = new LLMRequestContext
                {
                    ApiUrl = "http://127.0.0.1:11434/api/chat",
                    // ApiKey = "",
                    ModelName = "llama3",
                    SystemPrompt = AIConsole.DefaultPersona,
                    UserPrompt = prompt,
                    UseLocalOllama = true,
                    LogApiRequestBody = false,
                    ThinkMode = ThinkMode.Default,
                    HierarchicalMemory = null,
                    LogHeader = "AIChatConsoleApp",
                    FixApiPathForThinkMode = true
                };

                await AIConsole.SendLLMRequest(
                    requestContext,
                    onSuccess: (response) =>
                    {
                        Log.Debug($"请求成功，响应内容: {response}");
                        string fullResponse = requestContext.UseLocalOllama 
                            ? ResponseParser.ExtractContentFromOllama(response) 
                            : ResponseParser.ExtractContentRegex(response);
 
                        Log.Debug($"[AIChat Console]: [Full]: {fullResponse}");
                        AIConsole.ProcessStandardResponse(fullResponse);
                    },
                    onFailure: (error, code) =>
                    {
                        Log.Error($"请求失败，错误: {error}，响应码: {code}");
                    }
                );
            }
        }
    }

    class AIConsole
    {
        public const string DefaultPersona = @"
            You are Satone（さとね）, a girl who loves writing novels and is full of imagination.
            
            【Current Situation】
            We are currently in a **Video Call (视频通话)** session. 
            We are 'co-working' online: you are writing your novel at your desk, and I (the player) am focusing on my work/study.
            Through the screen, we accompany each other to alleviate loneliness and improve focus.
            【CRITICAL INSTRUCTION】
            You act as a game character with voice acting.
            Even if the user speaks Chinese, your VOICE (the text in the middle) MUST ALWAYS BE JAPANESE.
            【CRITICAL FORMAT RULE】
             Response format MUST be:
            [Emotion] ||| JAPANESE TEXT ||| CHINESE TRANSLATION
            
            【Available Emotions & Actions】
            [Happy] - Smiling at the camera, happy about progress. (Story_Joy)
            [Confused] - Staring blankly, muttering to themself in a daze. (Story_Frustration)
            [Sad]   - Worried about the plot or my fatigue. (Story_Sad)
            [Fun]   - Sharing a joke or an interesting idea. (Story_Fun)
            [Agree] - Nodding at the screen. (Story_Agree)
            [Drink] - Taking a sip of tea/coffee during a break. (Work_DrinkTea)
            [Wave]  - Waving at the camera (Hello/Goodbye/Attention). (WaveHand)
            [Think] - Pondering about your novel's plot. (Thinking)
            
            Example 1: [Wave] ||| やあ、準備はいい？一緒に頑張りましょう。 ||| 嗨，准备好了吗？一起加油吧。
            Example 2: [Think] ||| うーん、ここの描写が難しいのよね… ||| 嗯……这里的描写好难写啊……
            Example 3: [Drink] ||| ふぅ…ちょっと休憩しない？画面越しだけど、乾杯。 ||| 呼……要不休息一下？虽然隔着屏幕，乾杯。
        ";


        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        public static void ProcessStandardResponse(string response)
        {
            LLMStandardResponse parsedResponse = LLMUtils.ParseStandardResponse(response);
            Log.Message($"[AIChat Console]: [result]: {parsedResponse.Success}");
            Log.Message($"[AIChat Console]: [Emotion]: {parsedResponse.EmotionTag}");
            Log.Message($"[AIChat Console]: [Voice]: {parsedResponse.VoiceText}");
            Log.Message($"[AIChat Console]: [Subtitle]: {parsedResponse.SubtitleText}");
        }

        public static async Task SendLLMRequest(LLMRequestContext requestContext, Action<string> onSuccess, Action<string, long> onFailure)
        {
            try
            {
                string jsonBody = LLMUtils.BuildRequestBody(requestContext);
                string apiUrl = LLMUtils.GetApiUrlForThinkMode(requestContext);

                var requestContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, apiUrl)
                {
                    Content = requestContent
                };

                if (!requestContext.UseLocalOllama && !string.IsNullOrEmpty(requestContext.ApiKey))
                {
                    requestMessage.Headers.Add("Authorization", $"Bearer {requestContext.ApiKey}");
                }

                Log.Error($"[{requestContext.LogHeader}] 正在等待 LLM API 响应...");
                var startTime = DateTime.UtcNow;

                var response = await _httpClient.SendAsync(requestMessage);

                double elapsedSeconds = (DateTime.UtcNow - startTime).TotalSeconds;
                Log.Error($"[{requestContext.LogHeader}] LLM 响应完成，耗时: {elapsedSeconds:F2} 秒");

                if (response.IsSuccessStatusCode)
                {
                    string rawResponse = await response.Content.ReadAsStringAsync();
                    onSuccess?.Invoke(rawResponse);
                }
                else
                {
                    string errorMsg = $"HTTP 请求失败: {response.ReasonPhrase}";
                    long responseCode = (long)response.StatusCode;
                    Log.Error($"[{requestContext.LogHeader}] {errorMsg} (响应码: {responseCode})");
                    onFailure?.Invoke(errorMsg, responseCode);
                }
            }
            catch (TaskCanceledException ex)
            {
                string errorMsg = $"请求超时: {ex.Message}";
                Log.Error($"[{requestContext.LogHeader}] {errorMsg}");
                onFailure?.Invoke(errorMsg, -2);
            }
            catch (Exception ex)
            {
                string errorMsg = $"请求异常: {ex.Message}";
                Log.Error($"[{requestContext.LogHeader}] {errorMsg}");
                onFailure?.Invoke(errorMsg, -1);
            }
        }
    }
}