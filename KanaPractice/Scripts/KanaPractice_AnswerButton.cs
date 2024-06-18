
using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;

public class KanaPractice_AnswerButton : UdonSharpBehaviour
{
    private KanaPractice _sys;
    private Button _button;
    [NonSerialized] public byte mode = 0; // Represents whether the button will be treated as a hiragana or katakana answer button.
    private bool _isCorrect = false;
    [NonSerialized] public bool canScore = false; // While this bool is true, the button can affect the score.
    private Image _status;
    private Sprite _correctIcon;
    private Sprite _wrongIcon;

    void Start()
    {
        _sys = transform.root.GetComponent<KanaPractice>();
        _button = GetComponent<Button>();
        _status = transform.Find("Status").GetComponent<Image>();
        _correctIcon = _sys.GetCorrectIcon();
        _wrongIcon = _sys.GetWrongIcon();
    }

    // If the player immediately answers correctly, they gain a point.
    // If the player is wrong, they cannot gain points from the question anymore.
    public void CheckAnswer()
    {
        if (_isCorrect)
        {
            // Correct Answer
            _sys.DisableAnswers(); // Disable interaction with all buttons. This blocks player responses until the next question.

            _sys.PlaySFXCorrect();

            if (canScore)
            {
                _sys.IncreaseScore(); // Score increases. No button can change the score until the next question.
            }

            _status.sprite = _correctIcon;
            _status.enabled = true; // Show correct/incorrect status of the answer.

            switch(mode) // On correct answer, proceed to the next question. All status will be hidden again. Player responses will be allowed again.
            {
                case 0: // Hiragana
                {
                    _sys.SendCustomEventDelayedSeconds("PracticeHiraganaSessionNext", 1.0f);
                    break;
                }
                case 1: // Katakana
                {
                    _sys.SendCustomEventDelayedSeconds("PracticeKatakanaSessionNext", 1.0f);
                    break;
                }
                default:
                {
                    break;
                }
            }
        }
        else
        {
            DisableButton();

            _sys.PlaySFXWrong();

            if (canScore)
            {
                _sys.DisableScore(); // No button can change the score until the next question.
            }
            
            _status.sprite = _wrongIcon;
            _status.enabled = true; // Show correct/incorrect status of the answer.
        }
    }

    public void SetAsWrong()
    {
        _isCorrect = false;
    }

    public void SetAsCorrect()
    {
        _isCorrect = true;
    }

    public void HideStatus()
    {
        _status.enabled = false;
    }

    public void ShowStatus()
    {
        _status.enabled = true;
    }

    public void DisableButton()
    {
        _button.interactable = false;
    }

    public void EnableButton()
    {
        _button.interactable = true;
    }
}
