using Amazon;
using Amazon.Polly;
using Amazon.Polly.Model;
using Amazon.Runtime;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class TextToSpeech : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    public async void Speak(string reply)
    {
        if (string.IsNullOrEmpty(reply)) return;

        try
        {
            // ? 使用 AWS 預設憑證（從 .aws/credentials 讀取）
            var credentials = FallbackCredentialsFactory.GetCredentials();

            var client = new AmazonPollyClient(credentials, RegionEndpoint.EUCentral1);

            var request = new SynthesizeSpeechRequest()
            {
                Text = reply,
                Engine = Engine.Neural,
                VoiceId = VoiceId.Zhiyu,
                OutputFormat = OutputFormat.Mp3,
                LanguageCode = "cmn-CN"
            };

            var response = await client.SynthesizeSpeechAsync(request);

            WriteIntoFile(response.AudioStream);

            await PlayAudio();
        }
        catch (Exception e)
        {
            Debug.LogError($"TTS Error: {e.Message}");
        }
    }

    private async Task PlayAudio()
    {
        string path = $"{Application.persistentDataPath}/audio.mp3";

        using (var www = UnityWebRequestMultimedia.GetAudioClip(
            $"file://{path}",
            AudioType.MPEG))
        {
            var op = www.SendWebRequest();

            while (!op.isDone)
                await Task.Delay(100);

            if (www.result == UnityWebRequest.Result.Success)
            {
                if (audioSource != null)
                {
                    var clip = DownloadHandlerAudioClip.GetContent(www);
                    audioSource.pitch = 1.1f;
                    audioSource.clip = clip;
                    audioSource.Play();
                }
            }
            else
            {
                Debug.LogError("Failed to load audio clip");
            }
        }
    }

    private void WriteIntoFile(Stream stream)
    {
        string path = $"{Application.persistentDataPath}/audio.mp3";

        using (var fileStream = new FileStream(path, FileMode.Create))
        {
            stream.CopyTo(fileStream);
        }
    }
}