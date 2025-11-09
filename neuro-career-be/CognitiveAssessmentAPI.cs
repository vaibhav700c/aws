// Unity C# Script - VR Cognitive Assessment API Integration
// File: Assets/Scripts/AI/CognitiveAssessmentAPI.cs
// This script handles real-time performance data transmission from Unity VR to our AI backend

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Text;

namespace CognitiveAssessment.AI
{
    [System.Serializable]
    public class VRPerformanceData
    {
        public string session_id;
        public string player_id;
        public string scenario_type;
        public float duration_seconds;
        public int response_time_ms;
        public float accuracy_percentage;
        public int zones_completed;
        public int total_zones;
        public int errors_made;
        public int hints_used;
        public float gaze_focus_score;
        public float hand_tracking_precision;
        public StressIndicators stress_indicators;
        public string timestamp;
    }

    [System.Serializable]
    public class StressIndicators
    {
        public int heart_rate_avg;
        public float stress_level;
        public float engagement_score;
    }

    [System.Serializable]
    public class AICareerAnalysis
    {
        public string status;
        public string session_id;
        public string analysis;
        public CognitiveProfile cognitive_profile;
        public string[] recommended_careers;
        public string[] next_vr_scenarios;
        public float confidence_score;
        public int processing_time_ms;
        public string timestamp;
    }

    [System.Serializable]
    public class CognitiveProfile
    {
        public float problem_solving;
        public float response_speed;
        public float attention_focus;
        public float motor_precision;
        public float persistence;
    }

    public class CognitiveAssessmentAPI : MonoBehaviour
    {
        [Header("API Configuration")]
        [SerializeField] private string apiBaseUrl = "http://localhost:8000";
        [SerializeField] private float sendDataInterval = 30f; // Send data every 30 seconds
        
        [Header("VR Assessment Data")]
        [SerializeField] private string currentScenario = "problem_solving";
        [SerializeField] private int totalZones = 12;
        
        // Private fields
        private string sessionId;
        private string playerId;
        private int currentZone = 0;
        private float sessionStartTime;
        private List<float> responseTimes = new List<float>();
        private int totalErrors = 0;
        private int hintsUsed = 0;
        
        // VR-specific components (would be assigned in inspector)
        [Header("VR Tracking Components")]
        public EyeTrackingData eyeTracker;
        public HandTrackingManager handTracker;
        public HeartRateMonitor biometricSensor;
        
        void Start()
        {
            InitializeSession();
            StartCoroutine(PeriodicDataTransmission());
            
            Debug.Log($"[CognitiveAPI] VR Assessment Session Started: {sessionId}");
            Debug.Log($"[CognitiveAPI] Connected to AI Backend: {apiBaseUrl}");
        }
        
        void InitializeSession()
        {
            sessionId = $"vr_session_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
            playerId = $"player_{UnityEngine.Random.Range(1000, 9999)}";
            sessionStartTime = Time.time;
        }
        
        // Called when player completes a zone/task
        public void OnZoneCompleted(float completionTime, float accuracyScore, int errorsInZone)
        {
            currentZone++;
            responseTimes.Add(completionTime);
            totalErrors += errorsInZone;
            
            Debug.Log($"[CognitiveAPI] Zone {currentZone} completed - Accuracy: {accuracyScore}%");
            
            // Send real-time data to AI for immediate analysis
            StartCoroutine(SendPerformanceData());
        }
        
        // Called when player uses a hint
        public void OnHintUsed()
        {
            hintsUsed++;
            Debug.Log($"[CognitiveAPI] Hint used. Total hints: {hintsUsed}");
        }
        
        IEnumerator SendPerformanceData()
        {
            var performanceData = CollectCurrentPerformanceData();
            string jsonData = JsonConvert.SerializeObject(performanceData);
            
            Debug.Log($"[CognitiveAPI] Transmitting VR data to AI backend...");
            Debug.Log($"[CognitiveAPI] Payload size: {Encoding.UTF8.GetByteCount(jsonData)} bytes");
            
            using (UnityWebRequest www = new UnityWebRequest($"{apiBaseUrl}/api/unity-integration", "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                
                yield return www.SendWebRequest();
                
                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[CognitiveAPI] ✅ AI Analysis received successfully!");
                    
                    try
                    {
                        AICareerAnalysis analysis = JsonConvert.DeserializeObject<AICareerAnalysis>(www.downloadHandler.text);
                        ProcessAIAnalysis(analysis);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[CognitiveAPI] Failed to parse AI response: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"[CognitiveAPI] ❌ API Request failed: {www.error}");
                    Debug.LogError($"[CognitiveAPI] Response Code: {www.responseCode}");
                }
            }
        }
        
        VRPerformanceData CollectCurrentPerformanceData()
        {
            float avgResponseTime = responseTimes.Count > 0 ? 
                responseTimes[responseTimes.Count - 1] * 1000f : 2000f;
                
            float currentAccuracy = CalculateCurrentAccuracy();
            float gazeScore = eyeTracker ? eyeTracker.GetFocusScore() : UnityEngine.Random.Range(0.6f, 0.9f);
            float handPrecision = handTracker ? handTracker.GetPrecisionScore() : UnityEngine.Random.Range(0.7f, 0.95f);
            
            var stressData = new StressIndicators
            {
                heart_rate_avg = biometricSensor ? biometricSensor.GetAverageHeartRate() : UnityEngine.Random.Range(75, 110),
                stress_level = biometricSensor ? biometricSensor.GetStressLevel() : UnityEngine.Random.Range(0.2f, 0.7f),
                engagement_score = CalculateEngagementScore()
            };
            
            return new VRPerformanceData
            {
                session_id = sessionId,
                player_id = playerId,
                scenario_type = currentScenario,
                duration_seconds = Time.time - sessionStartTime,
                response_time_ms = Mathf.RoundToInt(avgResponseTime),
                accuracy_percentage = currentAccuracy,
                zones_completed = currentZone,
                total_zones = totalZones,
                errors_made = totalErrors,
                hints_used = hintsUsed,
                gaze_focus_score = gazeScore,
                hand_tracking_precision = handPrecision,
                stress_indicators = stressData,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
        }
        
        void ProcessAIAnalysis(AICareerAnalysis analysis)
        {
            Debug.Log($"[AI Analysis] Confidence: {analysis.confidence_score}%");
            Debug.Log($"[AI Analysis] Top Career: {analysis.recommended_careers[0]}");
            Debug.Log($"[AI Analysis] Processing Time: {analysis.processing_time_ms}ms");
            
            // Update UI with career recommendations
            UIManager.Instance.UpdateCareerRecommendations(analysis.recommended_careers);
            UIManager.Instance.ShowAnalysis(analysis.analysis);
            
            // Trigger next VR scenario based on AI recommendation
            if (analysis.next_vr_scenarios.Length > 0)
            {
                VRScenarioManager.Instance.QueueNextScenario(analysis.next_vr_scenarios[0]);
            }
        }
        
        float CalculateCurrentAccuracy()
        {
            if (currentZone == 0) return 100f;
            
            float errorRate = (float)totalErrors / (currentZone * 10f); // Assume 10 tasks per zone
            return Mathf.Clamp(100f - (errorRate * 100f), 0f, 100f);
        }
        
        float CalculateEngagementScore()
        {
            float timeEngaged = Time.time - sessionStartTime;
            float engagementDecay = Mathf.Clamp01(timeEngaged / 600f); // Decreases over 10 minutes
            return Mathf.Clamp01(1f - engagementDecay + UnityEngine.Random.Range(-0.1f, 0.1f));
        }
        
        IEnumerator PeriodicDataTransmission()
        {
            while (true)
            {
                yield return new WaitForSeconds(sendDataInterval);
                
                if (currentZone > 0) // Only send if player has started
                {
                    yield return StartCoroutine(SendPerformanceData());
                }
            }
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // Send final data before app pauses
                StartCoroutine(SendPerformanceData());
            }
        }
        
        void OnDestroy()
        {
            // Send final session data
            StartCoroutine(SendPerformanceData());
        }
    }
}

// Additional Helper Classes for VR Components

[System.Serializable]
public class EyeTrackingData : MonoBehaviour
{
    public float GetFocusScore()
    {
        // Simulate eye tracking focus score
        return UnityEngine.Random.Range(0.6f, 0.95f);
    }
}

[System.Serializable]  
public class HandTrackingManager : MonoBehaviour
{
    public float GetPrecisionScore()
    {
        // Simulate hand tracking precision
        return UnityEngine.Random.Range(0.7f, 0.98f);
    }
}

[System.Serializable]
public class HeartRateMonitor : MonoBehaviour  
{
    public int GetAverageHeartRate()
    {
        return UnityEngine.Random.Range(75, 110);
    }
    
    public float GetStressLevel()
    {
        return UnityEngine.Random.Range(0.2f, 0.8f);
    }
}