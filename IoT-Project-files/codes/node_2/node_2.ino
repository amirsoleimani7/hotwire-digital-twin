#include <ESP8266WiFi.h>
#include <PubSubClient.h>
#include <Wire.h>
#include <Adafruit_Sensor.h>
#include <Adafruit_BMP280.h>  // Use BMP280 instead of BME280

#define SEALEVELPRESSURE_HPA (1015.5) // example local sea level pressure

// I2C Pins
#define BMP_SDA 5  // D1 (GPIO5)
#define BMP_SCL 4  // D2 (GPIO4)

// Sonar Pins
const int trigPin_1 = 14;  // D5 (GPIO14)
const int echoPin_1 = 12;  // D6 (GPIO12)
const int trigPin_2 = 13;  // D7 (GPIO13)
const int echoPin_2 = 15;  // D8 (GPIO15)

Adafruit_BMP280 bmp;  // BMP280 object

// Timing Variables
unsigned long lastSend = 0;
const long sendInterval = 100;  // 100ms for all sensors

// WiFi & MQTT
const char* ssid = "testwifi";
const char* password = "naminami";
const char* mqtt_server = "192.168.78.177";

// MQTT Topics
const char* clientID = "ESP8266_Cube_2";
const char* sonarTopic = "cube/2/data";
const char* tempTopic = "cube/3/data";
const char* willTopic = "cube/2/data";
const char* willMessage = "offline";

WiFiClient espClient;
PubSubClient mqttClient(espClient);
void setup() {
  Serial.begin(9600);
  Serial.println("\nBooting Cube Sensor Board 2...");

  // Initialize sonar pins
  pinMode(trigPin_1, OUTPUT);
  pinMode(echoPin_1, INPUT);
  pinMode(trigPin_2, OUTPUT);
  pinMode(echoPin_2, INPUT);

  // Initialize I2C
  Wire.begin(BMP_SDA, BMP_SCL);
  delay(100);

  // --- I2C Scan for all devices ---
  Serial.println("Scanning I2C bus...");
  for (byte addr = 1; addr < 127; addr++) {
    Wire.beginTransmission(addr);
    if (Wire.endTransmission() == 0) {
      Serial.print("Found device at 0x");
      if (addr < 16) Serial.print("0");
      Serial.println(addr, HEX);
    }
  }

  // --- Try BMP280 Initialization ---
  if (!initializeBMP280()) {
    Serial.println("BMP280 initialization failed. Halting.");
    while (1);
  }

  bmp.setSampling(Adafruit_BMP280::MODE_NORMAL,
                Adafruit_BMP280::SAMPLING_X2,
                Adafruit_BMP280::SAMPLING_X16,
                Adafruit_BMP280::FILTER_X16,
                Adafruit_BMP280::STANDBY_MS_500);


  Serial.println("BMP280 initialization successful.");

  setup_wifi();
  mqttClient.setServer(mqtt_server, 1883);
  mqttClient.setCallback(callback);
}

// ... rest of code unchanged ...

bool initializeBMP280() {
  Serial.println("Initializing BMP280...");

  // Try default address (0x76)
  if (bmp.begin(0x76)) {
    Serial.println("Found BMP280 at 0x76");
    return true;
  }

  // Try alternative address (0x77)
  if (bmp.begin(0x77)) {
    Serial.println("Found BMP280 at 0x77");
    return true;
  }

  // Scan I2C if init failed
  Serial.println("Scanning I2C bus...");
  for (byte addr = 1; addr < 127; addr++) {
    Wire.beginTransmission(addr);
    if (Wire.endTransmission() == 0) {
      Serial.print("Found device at 0x");
      if (addr < 16) Serial.print("0");
      Serial.println(addr, HEX);
    }
  }

  return false;
}
void loop() {
  if (!mqttClient.connected()) {
    reconnect();
  }
  mqttClient.loop();

  unsigned long currentMillis = millis();

  if (currentMillis - lastSend >= sendInterval) {
    // Read sonar distances
    long dist1 = getSonarDistance(trigPin_1, echoPin_1);
    long dist2 = getSonarDistance(trigPin_2, echoPin_2);

    // Read BMP280 data
    float temp = bmp.readTemperature();
    float presRaw = bmp.readPressure();             // Pa
    float alt = bmp.readAltitude(SEALEVELPRESSURE_HPA); // meters

    // Check for NaNs
    if (isnan(temp)) Serial.println("Temperature is NaN");
    if (isnan(presRaw)) Serial.println("Pressure is NaN");
    if (isnan(alt)) Serial.println("Altitude is NaN");

    // Convert pressure to hPa
    float pres = presRaw / 100.0;

    // Debug prints
    Serial.print("Temperature (°C): "); Serial.println(temp);
    Serial.print("Raw Pressure (Pa): "); Serial.println(presRaw);
    Serial.print("Converted Pressure (hPa): "); Serial.println(pres);
    Serial.print("Altitude (m): "); Serial.println(alt);

    // Publish sonar data
    String sonarPayload = "{\"board\":2,\"sensors\":[";
    sonarPayload += "{\"id\":1,\"value\":" + String(dist1) + "},";
    sonarPayload += "{\"id\":2,\"value\":" + String(dist2) + "}";
    sonarPayload += "]}";
    mqttClient.publish(sonarTopic, sonarPayload.c_str());

    // Publish temperature, pressure, and altitude data
    String tempPayload = "{\"temp\":" + String(temp, 1) +
                         ",\"pressure\":" + String(pres, 1) +
                         ",\"altitude\":" + String(alt, 1) + "}";
    mqttClient.publish(tempTopic, tempPayload.c_str());

    // Print JSON payloads
    Serial.println("Sonar: " + sonarPayload);
    Serial.println("BMP280: " + tempPayload);

    lastSend = currentMillis;
  }

  delay(10);
}

long getSonarDistance(int trigPin, int echoPin) {
  digitalWrite(trigPin, LOW);
  delayMicroseconds(2);
  digitalWrite(trigPin, HIGH);
  delayMicroseconds(10);
  digitalWrite(trigPin, LOW);
  return pulseIn(echoPin, HIGH) * 0.034 / 2;
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
  // Handle incoming MQTT messages if needed
}

void reconnect() {
  while (!mqttClient.connected()) {
    Serial.print("Attempting MQTT connection...");
    if (mqttClient.connect(clientID, willTopic, 1, true, willMessage)) {
      Serial.println("connected");
      mqttClient.publish(willTopic, "online", true);
    } else {
      Serial.print("failed, rc=");
      Serial.print(mqttClient.state());
      Serial.println(" retrying in 5s");
      delay(5000);
    }
  }
}
