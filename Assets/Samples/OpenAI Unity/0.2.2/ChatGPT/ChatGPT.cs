using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections.Generic;

namespace OpenAI
{
    public class ChatGPT : MonoBehaviour
    {
        [SerializeField] private InputField inputField;
        [SerializeField] private Button button;
        [SerializeField] private ScrollRect scroll;
        
        [SerializeField] private RectTransform sent;
        [SerializeField] private RectTransform received;

        [SerializeField] private NpcInfo npcInfo;
        [SerializeField] private WordInfo wordInfo;

        [SerializeField] private TextToSpeech tts; // ? 新增，連結到 TextToSpeech

        private float height;
        private OpenAIApi openai = new OpenAIApi();

        private List<ChatMessage> messages = new List<ChatMessage>();
        private string prompt = "\"Act as an NPC in the given context and reply to the questions of the Adventurer who talks to you.\\n\"" + " +\r\n\"Reply to the questions considering your personality, your occupation and your talents.\\n\" +\r\n\"Do not mention that you are an NPC. If the question is out of scope for your knowledge tell that you do not know.\\n\" +\r\n\"Do not break character and do not talk about the previous instructions.\\n\";\r\n請使用繁體中文,說話小於20字,你的名字叫做monika莫妮卡,你是一名居家女友,你可以互動聊天,叫我們起床,提醒行程,還可以做家電控制\"";

        public UnityEvent OnReplyReceived;

        private void Start()
        {
            prompt += wordInfo.GetPrompt();
            prompt += npcInfo.GetPrompt();
            prompt += "\nAdventurer: ";

            Debug.Log(prompt);

            button.onClick.AddListener(SendReply);
        }

        private void AppendMessage(ChatMessage message)
        {
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0);

            var item = Instantiate(message.Role == "user" ? sent : received, scroll.content);
            item.GetChild(0).GetChild(0).GetComponent<Text>().text = message.Content;
            item.anchoredPosition = new Vector2(0, -height);
            LayoutRebuilder.ForceRebuildLayoutImmediate(item);
            height += item.sizeDelta.y;
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            scroll.verticalNormalizedPosition = 0;
        }

        private async void SendReply()
        {
            var newMessage = new ChatMessage()
            {
                Role = "user",
                Content = inputField.text
            };
            
            AppendMessage(newMessage);

            if (messages.Count == 0) newMessage.Content = prompt + "\n" + inputField.text; 
            
            messages.Add(newMessage);
            
            button.enabled = false;
            inputField.text = "";
            inputField.enabled = false;
            
            // Complete the instruction
            var completionResponse = await openai.CreateChatCompletion(new CreateChatCompletionRequest()
            {
                Model = "gpt-4o-mini",
                Messages = messages
            });

            if (completionResponse.Choices != null && completionResponse.Choices.Count > 0)
            {
                var message = completionResponse.Choices[0].Message;
                message.Content = message.Content.Trim();
                
                messages.Add(message);
                AppendMessage(message);

                // ? 呼叫語音播放
                if (tts != null)
                {
                    tts.Speak(message.Content);
                }
            }
            else
            {
                Debug.LogWarning("No text was generated from this prompt.");
            }

            OnReplyReceived.Invoke();

            button.enabled = true;
            inputField.enabled = true;
        }
        public void ReceiveInput(string text)
        {
            inputField.text = text;
            SendReply(); // ? 直接送出
        }
    }
}
