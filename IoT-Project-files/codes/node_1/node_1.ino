#include <ESP8266WiFi.h>
#include <PubSubClient.h>

// Sonar Pins
const int trigPin_1 = 5;   // D1 (GPIO5)
const int echoPin_1 = 4;   // D2 (GPIO4)
const int trigPin_2 = 14;  // D5 (GPIO14)
const int echoPin_2 = 12;  // D6 (GPIO12)

// WiFi & MQTT
const char* ssid = "testwifi";
const char* password = "naminami";
const char* mqtt_server = "192.168.78.177";

// Unique identifiers
const char* clientID = "ESP8266_Cube_1";
const char* topicBase = "cube/1/";
const char* willTopic = "cube/1/data";
const char* willMessage = "offline";

WiFiClient espClient;
PubSubClient mqttClient(espClient);

void setup() {
  Serial.begin(9600);
  Serial.println("\nBooting Cube Sensor Board 1...");

  // Initialize sonar pins
  pinMode(trigPin_1, OUTPUT);
  pinMode(echoPin_1, INPUT);
  pinMode(trigPin_2, OUTPUT);
  pinMode(echoPin_2, INPUT);

  setup_wifi();
  mqttClient.setServer(mqtt_server, 1883);
  mqttClient.setCallback(callback);
}

void loop() {
  if (!mqttClient.connected()) {
    reconnect();
  }
  mqttClient.loop();

  static unsigned long lastSend = 0;
  if (millis() - lastSend > 10) {
    // Read sensors

    long dist1 = getSonarDistance(trigPin_2, echoPin_2);
    long dist2 = getSonarDistance(trigPin_1, echoPin_1);
   
    dist1 = (-1 * dist1) + 56;
    dist2 = (-1 * dist2) + 54;
    

    // Create JSON payload with board ID
    String payload = "{\"board\":1,\"sensors\":[";
    payload += "{\"id\":1,\"value\":" + String(dist1) + "},";
    payload += "{\"id\":2,\"value\":" + String(dist2) + "}";
    payload += "]}";

    // Publish to board-specific topic
    mqttClient.publish((topicBase + String("data")).c_str(), payload.c_str());
    Serial.println("Published: " + payload);
    
    lastSend = millis();
  }
  delay(20);
}

long getSonarDistance(int trigPin, int echoPin) {
  digitalWrite(trigPin, LOW);
  delayMicroseconds(2);
  digitalWrite(trigPin, HIGH);
  delayMicroseconds(10);
  digitalWrite(trigPin, LOW);
  long duration = pulseIn(echoPin, HIGH);
  return duration * 0.034 / 2;
}

void setup_wifi() {
  delay(10);
  Serial.print("Connecting to ");
  Serial.println(ssid);

  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("\nWiFi connected");
  Serial.print("IP: ");
  Serial.println(WiFi.localIP());
}

void callback(char* topic, byte* payload, unsigned int length) {
  // Handle incoming messages if needed
}

void reconnect() {
  while (!mqttClient.connected()) {
    Serial.print("Attempting MQTT connection...");
    if (mqttClient.connect(clientID, willTopic, 1, true, willMessage)) {
      Serial.println("connected");
      mqttClient.publish(willTopic, "online", true);
      mqttClient.subscribe((topicBase + String("command")).c_str());
    } else {
      Serial.print("failed, rc=");
      Serial.print(mqttClient.state());
      Serial.println(" retrying in 5s");
      delay(5000);
    }
  }
}