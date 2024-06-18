
using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using VRC.SDKBase;
using VRC.Udon;

public class KanaPractice : UdonSharpBehaviour
{
    private Animator _animator;
    private readonly string _paramMainMenuMove = "MainMenu_Move";

    [Header("Audio")]
    [Tooltip("This sound effect plays when a menu button is clicked.")]
    [SerializeField] private AudioClip _SFXClick;
    [Tooltip("This sound effect plays when a correct answer is clicked.")]
    [SerializeField] private AudioClip _SFXCorrect;
    [Tooltip("This sound effect plays when a wrong answer is clicked.")]
    [SerializeField] private AudioClip _SFXWrong;
    private AudioSource SFX;

    [Space(20)]
    [Header("Main Menu")]
    [SerializeField] private GameObject _mainMenu;
    private Button[] _mainMenuButtons;

    [Space(20)]
    [Header("Practice System")]
    [SerializeField] private GameObject _practiceScreen;
    [SerializeField] private TextMeshProUGUI _question;
    [Tooltip("This question is visible when the user is practicing hiragana.")]
    [SerializeField] private string _questionHiragana = "What is this hiragana?";
    [Tooltip("This question is visible when the user is practicing katakana.")]
    [SerializeField] private string _questionKatakana = "What is this katakana?";
    [SerializeField] private TextMeshProUGUI _prompt;
    [SerializeField] private Transform _answerSelection;
    private KanaPractice_AnswerButton[] _answerButtons;
    private TextMeshProUGUI[] _answersText;
    [Tooltip("This sprite appears when the user selects a correct answer.")]
    [SerializeField] private Sprite CorrectIcon;
    [Tooltip("This sprite appears when the user selects a wrong answer.")]
    [SerializeField] private Sprite WrongIcon;
    [SerializeField] private TextMeshProUGUI _progress;

    [Space(20)]
    [Header("Scoring System")]
    [SerializeField] private GameObject _resultsScreen;
    [SerializeField] private TextMeshProUGUI _qualification;
    [Tooltip("If the player is above this percentage score, they receive a passing grade. If not, they receive a failing grade.")]
    [SerializeField] private float _passingScoreOutOf100 = 50;
    [Tooltip("This text appears when the user receives a passing grade.")]
    [SerializeField] private string _passText = "合格";
    [Tooltip("This text appears when the user receives a failing grade.")]
    [SerializeField] private string _failText = "不合格";
    [SerializeField] private TextMeshProUGUI _finalScore;
    private short _score = 0;
    private int _round = 0; // Represents the current round and current kana.

    // As of December 16, 2023, VRChat Udon cannot implement structures such as struct and Dictionary.
    
    [Space(20)]
    [Header("Characters")]
    [Tooltip("Romaji")]
    [SerializeField] private string[] _romajiSet = new string[] {
        "a", "i", "u", "e", "o",
        "ka", "ki", "ku", "ke", "ko",
        "sa", "shi", "su", "se", "so",
        "ta", "chi", "tsu", "te", "to",
        "na", "ni", "nu", "ne", "no",
        "ha", "hi", "fu", "he", "ho",
        "ma", "mi", "mu", "me", "mo",
        "ya", "yu", "yo",
        "ra", "ri", "ru", "re", "ro",
        "wa", "wo",
        "n",
        "ga", "gi", "gu", "ge", "go",
        "za", "ji", "zu", "ze", "zo",
        "da", "dji", "dzu", "de", "do",
        "ba", "bi", "bu", "be", "bo",
        "pa", "pi", "pu", "pe", "po"
    };
    [Tooltip("Hiragana")]
    [SerializeField] private char[] _hiraganaSet = new char[] {
        'あ', 'い', 'う', 'え', 'お',
        'か', 'き', 'く', 'け', 'こ',
        'さ', 'し', 'す', 'せ', 'そ',
        'た', 'ち', 'つ', 'て', 'と',
        'な', 'に', 'ぬ', 'ね', 'の',
        'は', 'ひ', 'ふ', 'へ', 'ほ',
        'ま', 'み', 'む', 'め', 'も',
        'や', 'ゆ', 'よ',
        'ら', 'り', 'る', 'れ', 'ろ',
        'わ', 'を',
        'ん',
        'が', 'ぎ', 'ぐ', 'げ', 'ご',
        'ざ', 'じ', 'ず', 'ぜ', 'ぞ',
        'だ', 'ぢ', 'づ', 'で', 'ど',
        'ば', 'び', 'ぶ', 'べ', 'ぼ',
        'ぱ', 'ぴ', 'ぷ', 'ぺ', 'ぽ'
    };
    [Tooltip("Katakana")]
    [SerializeField] private char[] _katakanaSet = new char[] {
        'ア', 'イ', 'ウ', 'エ', 'オ',
        'カ', 'キ', 'ク', 'ケ', 'コ',
        'サ', 'シ', 'ス', 'セ', 'ソ',
        'タ', 'チ', 'ツ', 'テ', 'ト',
        'ナ', 'ニ', 'ヌ', 'ネ', 'ノ',
        'ハ', 'ヒ', 'フ', 'ヘ', 'ホ',
        'マ', 'ミ', 'ム', 'メ', 'モ',
        'ヤ', 'ユ', 'ヨ',
        'ラ', 'リ', 'ル', 'レ', 'ロ',
        'ワ', 'ヲ',
        'ン',
        'ガ', 'ギ', 'グ', 'ゲ', 'ゴ',
        'ザ', 'ジ', 'ズ', 'ゼ', 'ゾ',
        'ダ', 'ヂ', 'ヅ', 'デ', 'ド',
        'バ', 'ビ', 'ブ', 'ベ', 'ボ',
        'パ', 'ピ', 'プ', 'ペ', 'ポ'
    };

#if UNITY_EDITOR
    private readonly string _nameTag = $"[<color=#66b3ff>Kana Practice</color>]";

    private void OnValidate()
    {
        // Settings Validation
        // When this component is modified in the Unity Inspector, this function will run.
        // As of December 19, 2023, VRChat Udon cannot: implement Try/Catch, implement Environment.Newline.

        // Character sets must be the same length.
        if ( (_hiraganaSet.Length != _katakanaSet.Length) && (_hiraganaSet.Length != _romajiSet.Length) )
        {
            Debug.LogError($"{_nameTag} Hiragana Set ({_hiraganaSet.Length}), Katakana Set ({_katakanaSet.Length}), and Romaji Set ({_romajiSet.Length}) must all have equal lengths.");
        }
        
        // Character sets must only have unique elements.
        ValidateSyllableSets(_romajiSet, "Romaji Set");
        ValidateSyllableSets(_hiraganaSet, "Hiragana Set");
        ValidateSyllableSets(_katakanaSet, "Katakana Set");
    }

    private void ValidateSyllableSets(char[] array, string name)
    {
        for (int i = 0; i < array.Length; i++)
        {
            for (int j = 0; j < array.Length; j++)
            {
                if (j == i) continue;

                if (array[i] == array[j])
                {
                    Debug.LogWarning($"{_nameTag} Duplicate element found in {name} at index {i}: {array[i]}");
                }
            }
        }
    }

    private void ValidateSyllableSets(string[] array, string name)
    {
        for (int i = 0; i < array.Length; i++)
        {
            for (int j = 0; j < array.Length; j++)
            {
                if (j == i) continue;

                if (array[i] == array[j])
                {
                    Debug.LogWarning($"{_nameTag} Duplicate element found in {name} at index {i}: {array[i]}");
                }
            }
        }
    }
#endif

    void Start()
    {
        // Initialization

        _animator = GetComponent<Animator>();
        SFX = GetComponent<AudioSource>();
        _mainMenuButtons = _mainMenu.GetComponentsInChildren<Button>();

        _answerButtons = new KanaPractice_AnswerButton[_answerSelection.childCount];
        _answersText = new TextMeshProUGUI[_answerButtons.Length];

        for (int i = 0; i < _answerButtons.Length; i++)
        {
            _answerButtons[i] = _answerSelection.GetChild(i).GetComponent<KanaPractice_AnswerButton>();
            _answersText[i] = _answerButtons[i].transform.GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    // Helper methods to make it possible for Unity events to modify animator parameters.

    public void MoveMainMenuDefault() // The main menu is in the viewport.
    {
        foreach (Button b in _mainMenuButtons)
        {
            b.interactable = true;
        }

        _animator.SetBool(_paramMainMenuMove, false);
    }

    public void MoveMainMenuOffset() // The main menu moves away. The practice screen enters the viewport.
    {
        foreach (Button b in _mainMenuButtons)
        {
            b.interactable = false;
        }

        DisplayPracticeScreen();

        _animator.SetBool(_paramMainMenuMove, true);
    }

    // Audio

    public void PlaySFXClick()
    {
        SFX.clip = _SFXClick;
        SFX.Play();
    }

    public void PlaySFXCorrect()
    {
        SFX.clip = _SFXCorrect;
        SFX.Play();
    }

    public void PlaySFXWrong()
    {
        SFX.clip = _SFXWrong;
        SFX.Play();
    }

    // Practice System

    public void DisableScore()
    {
        foreach (KanaPractice_AnswerButton b in _answerButtons)
        {
            b.canScore = false;
        }
    }

    public void IncreaseScore()
    {
        _score++;
        DisableScore();
    }

    public Sprite GetCorrectIcon()
    {
        return CorrectIcon;
    }

    public Sprite GetWrongIcon()
    {
        return WrongIcon;
    }

    public static void Shuffle(char[] array1, char[] array2, string[] array3)
    {
        // Algorithm based on Fisher-Yates shuffle.

        for (int i = array1.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1); // Under VRChat Udon, UnityEngine.Random.Range() will perform the role of System.Random.Next().

            char temp1 = array1[i];
            array1[i] = array1[j];
            array1[j] = temp1;

            char temp2 = array2[i];
            array2[i] = array2[j];
            array2[j] = temp2;

            string temp3 = array3[i];
            array3[i] = array3[j];
            array3[j] = temp3;
        }
    }

    private void ShuffleSyllabary()
    {
        // Shuffle all syllabary.
        Shuffle(_hiraganaSet, _katakanaSet, _romajiSet);
    }

    private void RandomizeAnswers()
    {
        int rng;

        // Fill all choices with incorrect answers.

        for (int a = 0; a < _answersText.Length; a++)
        {
            bool forLooping = true;
            while (true) // Validation. Warning: This will loop infinitely if there are less characters than the amount of possible answers.
            {
                rng = UnityEngine.Random.Range(0, _romajiSet.Length); // This generates a value within the bounds [minInclusive..maxExclusive).

                // Randomize again if the correct answer occurs.
                if (rng == _round)
                {
                    continue;
                }

                // Randomize again if a repeated value was generated.
                forLooping = true;

                for (int i = 0; forLooping && (i < a); i++) // Validate the generation against every currently existing answer.
                {
                    if (_romajiSet[rng] == _answersText[i].text)
                    {
                        forLooping = false;
                    }
                }

                if (!forLooping) // If the for-loop was terminated early, there was a repeated value.
                {
                    continue;
                }

                break;
            }

            _answersText[a].text = _romajiSet[rng].ToString();
            _answerButtons[a].SendCustomEvent("SetAsWrong");
        }

        // Insert the correct answer into a random choice.

        rng = UnityEngine.Random.Range(0, _answersText.Length);
        _answersText[rng].text = _romajiSet[_round];
        _answerButtons[rng].SendCustomEvent("SetAsCorrect");
    }

    public void DisableAnswers()
    {
        foreach (KanaPractice_AnswerButton b in _answerButtons)
        {
            b.DisableButton();
        }
    }

    public void PracticeHiragana()
    {
        _round = 0;
        _score = 0;

        ShuffleSyllabary();
        
        _question.text = _questionHiragana;
        
        foreach (KanaPractice_AnswerButton b in _answerButtons)
        {
            b.mode = 0; // Causes buttons to proceed with hiragana questions.
        }

        PracticeHiraganaSession();
    }

    public void PracticeHiraganaSession()
    {
        _progress.text = $"{_round + 1}/{_hiraganaSet.Length}";

        _prompt.text = _hiraganaSet[_round].ToString();
        RandomizeAnswers();

        foreach (KanaPractice_AnswerButton b in _answerButtons)
        {
            b.HideStatus();
            b.canScore = true;
            b.EnableButton();
        }
    }

    public void PracticeHiraganaSessionNext()
    {
        if ( (_round + 1) == _hiraganaSet.Length )
        {
            // Display results.
            DisplayResultsScreen();

            return;
        }

        _round++;
        
        PracticeHiraganaSession();
    }

    public void PracticeKatakana()
    {
        _round = 0;
        _score = 0;

        ShuffleSyllabary();
        
        _question.text = _questionKatakana;
        
        foreach (KanaPractice_AnswerButton b in _answerButtons)
        {
            b.mode = 1; // Causes buttons to proceed with katakana questions.
        }

        PracticeKatakanaSession();
    }

    public void PracticeKatakanaSession()
    {
        _progress.text = $"{_round + 1}/{_katakanaSet.Length}";

        _prompt.text = _katakanaSet[_round].ToString();
        RandomizeAnswers();

        foreach (KanaPractice_AnswerButton b in _answerButtons)
        {
            b.HideStatus();
            b.canScore = true;
            b.EnableButton();
        }
    }

    public void PracticeKatakanaSessionNext()
    {
        if ( (_round + 1) == _katakanaSet.Length )
        {
            // Display results.
            DisplayResultsScreen();

            return;
        }

        _round++;

        PracticeKatakanaSession();
    }

    public void DisplayPracticeScreen()
    {
        _practiceScreen.SetActive(true);
        _resultsScreen.SetActive(false);
    }

    public void DisplayResultsScreen()
    {
        _practiceScreen.SetActive(false);

        // As of December 18, 2023, VRChat Udon cannot use System.MathF.Round().
        float percentageScore =  ( (float)_score / (float)_romajiSet.Length ) * 100;

        _qualification.text = (percentageScore > _passingScoreOutOf100) ? _passText : _failText;
        _finalScore.text = _score.ToString() + "/" + _romajiSet.Length + " " + "(" + percentageScore.ToString("F2") + "%" + ")";

        _resultsScreen.SetActive(true);
    }
}

[CustomEditor( typeof(KanaPractice) )]
public class CustomUI : Editor
{
    private bool debugFoldout = false;

    private bool kanaFoldout = false;
    private SerializedProperty _propRomajiSet;
    private SerializedProperty _propHiraganaSet;
    private SerializedProperty _propKatakanaSet;

    private void OnEnable()
    {
        _propRomajiSet = serializedObject.FindProperty("_romajiSet");
        _propHiraganaSet = serializedObject.FindProperty("_hiraganaSet");
        _propKatakanaSet = serializedObject.FindProperty("_katakanaSet");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        kanaFoldout = EditorGUILayout.Foldout(kanaFoldout, "Kana", true);

        if (kanaFoldout)
        {
            for (int i = 0; i < _propRomajiSet.arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();

                SerializedProperty romajiElem = _propRomajiSet.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(romajiElem, GUIContent.none);

                SerializedProperty hiraganaElem = _propHiraganaSet.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(hiraganaElem, GUIContent.none);

                SerializedProperty katakanaElem = _propKatakanaSet.GetArrayElementAtIndex(i);
                EditorGUILayout.PropertyField(katakanaElem, GUIContent.none);

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        debugFoldout = EditorGUILayout.Foldout(debugFoldout, "[Default Inspector]", true);

        if (debugFoldout)
        {
            DrawDefaultInspector();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
