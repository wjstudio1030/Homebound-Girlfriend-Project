using Amazon;
using Amazon.Polly;
using Amazon.Polly.Model;
using Amazon.Runtime;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class TextToSpeech : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    // ? 直接呼叫這個方法播放語音
    public async void Speak(string reply)
    {
        if (string.IsNullOrEmpty(reply)) return;

        var credentials = new BasicAWSCredentials(
            accessKey: "AKIASK5MCLEKRDSH4UXM",
            secretKey: "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"
        );
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

        using (var www = UnityWebRequestMultimedia.GetAudioClip(
            $"file://{Application.persistentDataPath}/audio.mp3",
            AudioType.MPEG))
        {
            var op = www.SendWebRequest();

            while (!op.isDone)
            {
                await Task.Delay(100);
            }

            if (www.result == UnityWebRequest.Result.Success)
            {
                if (audioSource != null)
                {
                    var clip = DownloadHandlerAudioClip.GetContent(www);
                    audioSource.pitch = 1.1f; // 播放速度稍快
                    audioSource.clip = clip;
                    audioSource.Play();
                }
            }
            else
            {
                Debug.LogError("Failed to download audio clip");
            }
        }
    }

    private void WriteIntoFile(Stream stream)
    {
        using (var filestream = new FileStream(
            $"{Application.persistentDataPath}/audio.mp3", FileMode.Create))
        {
            byte[] buffer = new byte[8 * 1024];
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                filestream.Write(buffer, 0, bytesRead);
            }
        }
    }
}
