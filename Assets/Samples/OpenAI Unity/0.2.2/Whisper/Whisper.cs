using OpenAI;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Samples.Whisper
{
    public class Whisper : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Button recordButton;
        [SerializeField] private Image progressBar;
        [SerializeField] private Text message;
        [SerializeField] private Dropdown dropdown;

        [Header("Recording")]
        private readonly string fileName = "output.wav";
        private readonly int duration = 5;

        private AudioClip clip;
        private bool isRecording;
        private float time;

        private OpenAIApi openai;

        private void Awake()
        {
            string apiKey =
                Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.User);

            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("OPENAI_API_KEY not found");
                return;
            }

            openai = new OpenAIApi(apiKey);
        }
        private void Start()
        {
            // ✅ Android 麥克風權限
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                Application.RequestUserAuthorization(UserAuthorization.Microphone);
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            dropdown.options.Add(
                new Dropdown.OptionData("Microphone not supported on WebGL"));
#else
            foreach (var device in Microphone.devices)
            {
                dropdown.options.Add(new Dropdown.OptionData(device));
            }

            recordButton.onClick.AddListener(StartRecording);
            dropdown.onValueChanged.AddListener(ChangeMicrophone);

            var index = PlayerPrefs.GetInt("user-mic-device-index", 0);
            dropdown.SetValueWithoutNotify(index);
#endif
        }

        private void ChangeMicrophone(int index)
        {
            PlayerPrefs.SetInt("user-mic-device-index", index);
        }

        private void StartRecording()
        {
            if (Microphone.devices.Length == 0)
            {
                message.text = "No microphone found";
                return;
            }

            isRecording = true;
            recordButton.enabled = false;
            time = 0;

#if !UNITY_WEBGL
            var index = PlayerPrefs.GetInt("user-mic-device-index", 0);
            clip = Microphone.Start(
                dropdown.options[index].text,
                false,
                duration,
                44100);
#endif
        }

        private async void EndRecording()
        {
            message.text = "Transcripting...";

#if !UNITY_WEBGL
            Microphone.End(null);
#endif

            byte[] data = SaveWav.Save(fileName, clip);

            var req = new CreateAudioTranscriptionsRequest
            {
                FileData = new FileData
                {
                    Data = data,
                    Name = "audio.wav"
                },
                Model = "whisper-1",
                Language = "zh"
            };

            try
            {
                var res = await openai.CreateAudioTranscription(req);

                message.text = res.Text;
                progressBar.fillAmount = 0;
                recordButton.enabled = true;

                // ✅ 丟給 ChatGPT（如果存在）
                var chat = FindObjectOfType<OpenAI.ChatGPT>();
                if (chat != null && !string.IsNullOrEmpty(res.Text))
                {
                    chat.ReceiveInput(res.Text);
                }
            }
            catch (System.Exception e)
            {
                message.text = "Whisper failed";
                Debug.LogError(e);
                recordButton.enabled = true;
            }
        }

        private void Update()
        {
            if (!isRecording) return;

            time += Time.deltaTime;
            progressBar.fillAmount = time / duration;

            if (time >= duration)
            {
                isRecording = false;
                EndRecording();
            }
        }
    }
}
