using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

public class MqttBasicClient : MonoBehaviour
{
    private MqttClient client;

    [Header("MQTT Settings")]
    public string brokerAddress = "10.45.176.153";
    public string topic = "first/test";

    [Header("TTS")]
    public TextToSpeech textToSpeech;

    // ?? 主執行緒佇列
    private Queue<Action> mainThreadActions = new Queue<Action>();

    // ========================
    // ?? atHome 狀態機
    // ========================
    private int lastAtHome = -1;
    private bool atHomeInitialized = false;

    // ========================
    // ??? Temperature 狀態機
    // ========================
    private enum TempState { None, Cold, Normal, Hot }
    private TempState lastTempState = TempState.None;

    // ========================
    // ?? Humidity 狀態機
    // ========================
    private enum HumiState { None, Dry, Normal, Wet }
    private HumiState lastHumiState = HumiState.None;

    // ========================
    // ?? Light (lux) 狀態機
    // ========================
    private enum LuxState { None, Dark, Normal, Bright }
    private LuxState lastLuxState = LuxState.None;
    private bool luxInitialized = false;

    // ========================
    // ?? Fire 狀態機
    // ========================
    private int lastFire = -1; // -1 表示尚未初始化

    void Start()
    {
        client = new MqttClient(brokerAddress);
        client.MqttMsgPublishReceived += OnMessageReceived;

        string clientId = Guid.NewGuid().ToString();
        client.Connect(clientId);

        if (client.IsConnected)
        {
            Debug.Log("? MQTT Connected");

            client.Subscribe(
                new string[] { topic },
                new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE }
            );

            Debug.Log($"?? Subscribed to topic: {topic}");

            if (textToSpeech != null)
            {
                textToSpeech.Speak("大家好我是莫妮卡居家女友");
            }
        }
        else
        {
            Debug.LogError("? MQTT Connection Failed");
        }
    }

    void Update()
    {
        while (mainThreadActions.Count > 0)
        {
            mainThreadActions.Dequeue()?.Invoke();
        }
    }

    private void OnMessageReceived(object sender, MqttMsgPublishEventArgs e)
    {
        string message = Encoding.UTF8.GetString(e.Message);
        Debug.Log($"?? {message}");

        try
        {
            SensorData data = JsonUtility.FromJson<SensorData>(message);
            if (data == null) return;

            HandleAtHome(data.atHome);
            HandleTemperature(data.temp);
            HandleHumidity(data.humi);
            HandleLux(data.lux);
            HandleFire(data.fire);
        }
        catch (Exception ex)
        {
            Debug.LogError("? JSON Parse Failed: " + ex.Message);
        }
    }

    // ========================
    // ?? atHome 處理
    // ========================
    private void HandleAtHome(int currentAtHome)
    {
        if (!atHomeInitialized)
        {
            lastAtHome = currentAtHome;
            atHomeInitialized = true;
            Debug.Log($"?? 初始化 atHome={currentAtHome}（不播音）");
            return;
        }

        if (currentAtHome == lastAtHome) return;

        lastAtHome = currentAtHome;

        if (currentAtHome == 1)
            EnqueueSpeak("家庭成員歡迎回來");
        else
            EnqueueSpeak("再見，路上小心");
    }

    // ========================
    // ??? Temperature 處理
    // ========================
    private void HandleTemperature(float temp)
    {
        TempState currentState;

        if (temp < 23)
            currentState = TempState.Cold;
        else if (temp > 29)
            currentState = TempState.Hot;
        else
            currentState = TempState.Normal;

        // 第一次就要念
        if (lastTempState == TempState.None)
        {
            lastTempState = currentState;
            SpeakTemp(currentState);
            return;
        }

        if (currentState == lastTempState) return;

        lastTempState = currentState;
        SpeakTemp(currentState);
    }

    private void SpeakTemp(TempState state)
    {
        if (state == TempState.Cold)
            EnqueueSpeak("天氣變冷了，要多穿幾件衣服");
        else if (state == TempState.Hot)
            EnqueueSpeak("主人天氣熱了，要幫您脫衣服嗎");
    }

    // ========================
    // ?? Humidity 處理
    // ========================
    private void HandleHumidity(float humi)
    {
        HumiState currentState;

        if (humi > 90)
            currentState = HumiState.Wet;
        else if (humi < 50)
            currentState = HumiState.Dry;
        else
            currentState = HumiState.Normal;

        // 第一次就要念
        if (lastHumiState == HumiState.None)
        {
            lastHumiState = currentState;
            SpeakHumi(currentState);
            return;
        }

        if (currentState == lastHumiState) return;

        lastHumiState = currentState;
        SpeakHumi(currentState);
    }

    private void SpeakHumi(HumiState state)
    {
        if (state == HumiState.Wet)
            EnqueueSpeak("濕度過高，注意灰塵滋生與過敏問題");
        else if (state == HumiState.Dry)
            EnqueueSpeak("現在太過乾燥，注意皮膚水分流失，多補充水分");
    }

    // ========================
    // ?? Lux 處理
    // ========================
    private void HandleLux(float lux)
    {
        LuxState currentState;

        if (lux > 700)
            currentState = LuxState.Bright;
        else if (lux < 350)
            currentState = LuxState.Dark;
        else
            currentState = LuxState.Normal;

        // 第一次收到 → 只初始化，不播音
        if (!luxInitialized)
        {
            lastLuxState = currentState;
            luxInitialized = true;
            Debug.Log($"?? 初始化 Lux={lux}（不播音）");
            return;
        }

        if (currentState == lastLuxState) return;

        lastLuxState = currentState;

        if (currentState == LuxState.Bright)
            EnqueueSpeak("現在太亮啦，我幫你自動關燈呀");
        else if (currentState == LuxState.Dark)
            EnqueueSpeak("現在太暗啦，我幫你自動開燈呀");
    }

    // ========================
    // ?? Fire 處理
    // ========================
    private void HandleFire(int fire)
    {
        // 第一次收到
        if (lastFire == -1)
        {
            lastFire = fire;
            if (fire == 1)
                SpeakFire();
            return;
        }

        if (fire == lastFire) return;

        lastFire = fire;

        if (fire == 1)
            SpeakFire();
    }

    private void SpeakFire()
    {
        EnqueueSpeak("發生火災,趕緊切斷火源，盡速逃離！");
    }

    // ========================
    // ?? 主執行緒播放
    // ========================
    private void EnqueueSpeak(string text)
    {
        mainThreadActions.Enqueue(() =>
        {
            if (textToSpeech != null)
            {
                Debug.Log($"?? 播放語音：{text}");
                textToSpeech.Speak(text);
            }
        });
    }

    private void OnDestroy()
    {
        if (client != null && client.IsConnected)
        {
            client.Disconnect();
        }
    }

    // ========================
    // ?? MQTT JSON 結構
    // ========================
    [Serializable]
    private class SensorData
    {
        public int fire;
        public float lux;
        public float temp;
        public float humi;
        public int atHome;
    }
}
