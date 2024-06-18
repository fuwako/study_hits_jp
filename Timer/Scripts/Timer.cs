
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;
using TMPro;
using System;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class Timer : UdonSharpBehaviour
{
    [UdonSynced(UdonSyncMode.None)] private float _minutes;
    [UdonSynced(UdonSyncMode.None)] private float _seconds;

    [UdonSynced(UdonSyncMode.None)] private bool _timerIsRunning = false;
    [UdonSynced(UdonSyncMode.None)] private bool _stopwatchIsRunning = false;

    [UdonSynced(UdonSyncMode.None)] private float _startTime;
    private float _totalSeconds;
    private float _displayTotalSeconds;
    private int _displayMinutes;
    private int _displaySeconds;

    [SerializeField] private InputField _minutesField;
    [SerializeField] private InputField _secondsField;

    private GraphicRaycaster _minutesFieldGRaycaster;
    private GraphicRaycaster _secondsFieldGRaycaster;

    [SerializeField] private TextMeshProUGUI _minutesTextDisplay;
    [SerializeField] private TextMeshProUGUI _secondsTextDisplay;

    [SerializeField] private Button _buttonTimer;
    [SerializeField] private Button _buttonStopwatch;
    [SerializeField] private Button _buttonStop;

    private VRCPlayerApi _localPlayer;

    void Start()
    {
        _minutesFieldGRaycaster = _minutesField.GetComponent<GraphicRaycaster>();
        _secondsFieldGRaycaster = _secondsField.GetComponent<GraphicRaycaster>();

        _localPlayer = Networking.LocalPlayer;
    }

    void Update()
    {
        if (_timerIsRunning)
        {
            // Count down from the user-specified time.
            _displayTotalSeconds = _totalSeconds - (Time.time - _startTime);

            _displayMinutes = (int)(_displayTotalSeconds / 60); // Only display whole numbers. Eliminate the decimal.
            _displaySeconds = (int)(_displayTotalSeconds % 60);

            // Display
            _minutesTextDisplay.text = _displayMinutes.ToString("00");
            _secondsTextDisplay.text = _displaySeconds.ToString("00");

            // Automatically end at 00:00.
            if ( (_displayMinutes == 0) && (_displaySeconds == 0) )
            {
                StopMode();
            }
        }
        else if (_stopwatchIsRunning)
        {
            // Count up from zero.
            _displayTotalSeconds = Time.time - _startTime;

            _displayMinutes = (int)(_displayTotalSeconds / 60); // Only display whole numbers. Eliminate the decimal.
            _displaySeconds = (int)(_displayTotalSeconds % 60);

            // Display
            _displayMinutes = (_displayMinutes < 0) ? 0 : _displayMinutes; // Zeroing for graphical error caused by slow synchronization.
            _displaySeconds = (_displaySeconds < 0) ? 0 : _displaySeconds;

            _minutesTextDisplay.text = _displayMinutes.ToString("00");
            _secondsTextDisplay.text = _displaySeconds.ToString("00");

            // Automatically end at 99:59.
            if ( (_displayMinutes == 99) && (_displaySeconds == 59) )
            {
                StopMode();
            }
        }
    }




    public void SetMinutes()
    {
        TakeOwnership();

        // Verify input.
        if ( string.IsNullOrEmpty(_minutesField.text) )
        {
            _minutes = 0;
        }
        else
        {
            _minutes = Convert.ToInt16(_minutesField.text);
            _minutes = Mathf.Clamp(_minutes, 0, 99);
        }

        DoOwnerRequestSerialization();

        // Display.
        _minutesTextDisplay.text = _minutes.ToString("00");
    }

    public void SetSeconds()
    {
        TakeOwnership();

        // Verify input.
        if ( string.IsNullOrEmpty(_secondsField.text) )
        {
            _seconds = 0;
        }
        else
        {
            _seconds = Convert.ToInt16(_secondsField.text);
            _seconds = Mathf.Clamp(_seconds, 0, 59);
        }

        DoOwnerRequestSerialization();

        // Display.
        _secondsTextDisplay.text = _seconds.ToString("00");
    }

    private void SetTimeInputEnabled(bool state)
    {
        // Setting the "interactable" or "enabled" properties of InputField doesn't prevent invoking VRChat's keyboard input.
        _minutesFieldGRaycaster.enabled = state;
        _secondsFieldGRaycaster.enabled = state;
    }




    public void StartTimerNetworked()
    {
        TakeOwnership();
        SendCustomNetworkEvent(NetworkEventTarget.All, "StartTimer");
    }
    
    public void StartTimer()
    {
        // Controller Display
        SetTimeInputEnabled(false);
        _buttonTimer.gameObject.SetActive(false);
        _buttonStopwatch.gameObject.SetActive(false);
        _buttonStop.gameObject.SetActive(true);

        // Initialize values for calculation of elapsed time.
        _startTime = Time.time;
        _totalSeconds = (_minutes * 60) + _seconds;

        // Start timer.
        _timerIsRunning = true;
        DoOwnerRequestSerialization();
    }




    public void StartStopwatchNetworked()
    {
        TakeOwnership();
        SendCustomNetworkEvent(NetworkEventTarget.All, "StartStopwatch");
    }
    
    public void StartStopwatch()
    {
        // Controller Display
        SetTimeInputEnabled(false);
        _buttonTimer.gameObject.SetActive(false);
        _buttonStopwatch.gameObject.SetActive(false);
        _buttonStop.gameObject.SetActive(true);

        // Initialize values for calculation of elapsed time.
        _startTime = Time.time;

        // Start timer.
        _stopwatchIsRunning = true;
        DoOwnerRequestSerialization();
    }




    public void StopModeNetworked()
    {
        TakeOwnership();
        SendCustomNetworkEvent(NetworkEventTarget.All, "StopMode");
    }

    public void StopMode()
    {
        // Controller Display
        SetTimeInputEnabled(true);
        _buttonTimer.gameObject.SetActive(true);
        _buttonStopwatch.gameObject.SetActive(true);
        _buttonStop.gameObject.SetActive(false);

        // Stop either mode.
        _timerIsRunning = false;
        _stopwatchIsRunning = false;

        // Save displayed values into variables.
        _minutes = _displayMinutes;
        _seconds = _displaySeconds;
        DoOwnerRequestSerialization();
    }




    private void TakeOwnership()
    {
        Networking.SetOwner(_localPlayer, this.gameObject);
    }

    private void DoOwnerRequestSerialization()
    {
        if ( _localPlayer == Networking.GetOwner(this.gameObject) )
        {
            RequestSerialization();
        }
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        DoOwnerRequestSerialization();
    }

    public override void OnDeserialization()
    {
        _minutesTextDisplay.text = _minutes.ToString("00");
        _secondsTextDisplay.text = _seconds.ToString("00");

        if (_timerIsRunning)
        {
            // Start timer.

            // Controller Display
            SetTimeInputEnabled(false);
            _buttonTimer.gameObject.SetActive(false);
            _buttonStopwatch.gameObject.SetActive(false);
            _buttonStop.gameObject.SetActive(true);

            // Initialize values for calculation of elapsed time.
            _totalSeconds = (_minutes * 60) + _seconds;
        }
        else if (_stopwatchIsRunning)
        {
            // Start stopwatch.

            // Controller Display
            SetTimeInputEnabled(false);
            _buttonTimer.gameObject.SetActive(false);
            _buttonStopwatch.gameObject.SetActive(false);
            _buttonStop.gameObject.SetActive(true);
        }
    }
}
