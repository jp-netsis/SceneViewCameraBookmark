using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace jp.netsis.Utility
{
    // Can't set transform of scene camera.
    // It internally updates every frame:
    //      cam.position = pivot + rotation * new Vector3(0, 0, -cameraDistance)
    // Info: https://forum.unity.com/threads/moving-scene-scene_view-camera-from-editor-script.64920/#post-3388397
    [Serializable]
    class SceneViewCameraBookmark
    {
        public string _name;
        public bool _orthographic;
        public Vector3 _pivot;
        public Quaternion _rotation;
        public float _size;

        public SceneViewCameraBookmark()
        {
            _name = String.Empty;
            _orthographic = false;
            _pivot = Vector3.zero;
            _rotation = Quaternion.identity;
            _size = 0f;
        }
        public SceneViewCameraBookmark(string name, SceneView sceneView)
        {
            _name = name;
            _orthographic = sceneView.orthographic;
            _pivot = sceneView.pivot;
            _rotation = sceneView.rotation;
            _size = sceneView.size;
        }
    }

    static class SceneViewCameraBookmarkHolder
    {
        private static string KEY = "jp.netsis.Utility.SceneViewCameraBookmark";
        private static Dictionary<int,SceneViewCameraBookmark> _bookmarkDic = new Dictionary<int,SceneViewCameraBookmark>();
        public static Dictionary<int, SceneViewCameraBookmark> BookmarkDic => _bookmarkDic;

        static JsonDictionary<int, SceneViewCameraBookmark> GetJsonDictionary()
        {
            JsonDictionary<int,SceneViewCameraBookmark> jsonDictionary;
            var json = EditorPrefs.GetString(KEY);
            if (string.IsNullOrEmpty(json))
            {
                jsonDictionary = new JsonDictionary<int, SceneViewCameraBookmark>(new Dictionary<int, SceneViewCameraBookmark>());
            }
            else
            {
                jsonDictionary = JsonUtility.FromJson<JsonDictionary<int,SceneViewCameraBookmark>>(json);
            }

            return jsonDictionary;
        }
        public static void Save(int slot,SceneViewCameraBookmark sceneViewCameraBookmark)
        {
            JsonDictionary<int,SceneViewCameraBookmark> jsonDictionary = GetJsonDictionary();
            
            if (jsonDictionary.Dictionary.ContainsKey(slot))
            {
                jsonDictionary.Dictionary[slot] = sceneViewCameraBookmark;
            }
            else
            {
                jsonDictionary.Dictionary.Add(slot,sceneViewCameraBookmark);
            }

            var json = JsonUtility.ToJson(jsonDictionary);
            EditorPrefs.SetString(KEY,json);
            _bookmarkDic = jsonDictionary.Dictionary;
        }
        public static Dictionary<int,SceneViewCameraBookmark> LoadAll()
        {
            JsonDictionary<int,SceneViewCameraBookmark> jsonDictionary = GetJsonDictionary();
            _bookmarkDic = jsonDictionary.Dictionary;
            return _bookmarkDic;
        }

        public static void Remove(int slot)
        {
            JsonDictionary<int,SceneViewCameraBookmark> jsonDictionary = GetJsonDictionary();
            
            if (jsonDictionary.Dictionary.ContainsKey(slot))
            {
                jsonDictionary.Dictionary.Remove(slot);
            }

            var json = JsonUtility.ToJson(jsonDictionary);
            EditorPrefs.SetString(KEY,json);
            _bookmarkDic = jsonDictionary.Dictionary;
        }
        public static void AllRemove()
        {
            EditorPrefs.DeleteKey(KEY);
            JsonDictionary<int,SceneViewCameraBookmark> jsonDictionary = GetJsonDictionary();
            _bookmarkDic = jsonDictionary.Dictionary;
        }
    }

    [Serializable]
    class SceneViewCameraMenu
    {
        public bool isEnable;
    }

    static class SceneViewCameraMenuHolder
    {
        private static string KEY = "jp.netsis.Utility.SceneViewCameraMenu";
        public static void Save(bool isEnable)
        {
            var saveObj = new SceneViewCameraMenu { isEnable = isEnable };
            var json = JsonUtility.ToJson(saveObj);
            EditorPrefs.SetString(KEY,json);
        }
        public static bool Load()
        {
            var json = EditorPrefs.GetString(KEY);
            var saveObj = JsonUtility.FromJson<SceneViewCameraMenu>(json);
            return (saveObj == null ? false : saveObj.isEnable); // not found value -> default(T)
        }
    }

    [InitializeOnLoad]
    public class SceneViewCameraGUI : EditorWindow
    {
        const string MENU_NAME = "Window/SceneViewCamera GUI";
        
        const float WINDOW_WIDTH = 170f;
        const float WINDOW_HEIGHT = 150f;
        const float WINDOW_OFFSET_Y = 50f;

        static readonly string[] _dropDownList = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "0" };

        static SceneViewCameraGUI _this;
        static int _selectIndex;
        static bool _isInitialized;
        static bool _isInputMode;
        static Rect _windowRect = new Rect (0, 0, WINDOW_WIDTH, WINDOW_HEIGHT);
        static SceneView _sceneView;
        
        static string _name = String.Empty;
        static GUIContent _orthoGuiContent = new GUIContent();
        static GUIContent _pivotGuiContent = new GUIContent();
        static GUIContent _rotateGuiContent = new GUIContent();
        static GUIContent _sizeGuiContent = new GUIContent();

        static GUIContent _inputButtonGuiContent = new GUIContent();
        static bool _inputModeOrthographic;
        static Vector3 _inputModePivot;
        static Quaternion _inputModeRotation;
        static float _inputModeSize;

        static SceneViewCameraGUI()
        {
            _this = _this;
            var isEnable = SceneViewCameraMenuHolder.Load();
            if (isEnable)
            {
                OnSceneGui(isEnable);
            }
        }

        [MenuItem(MENU_NAME)]
        public static void MenuSelect()
        {
            var isChecked = Menu.GetChecked(MENU_NAME);
            var nextChecked = !isChecked;
            Menu.SetChecked(MENU_NAME, nextChecked);
            OnSceneGui(nextChecked);
        }

        [MenuItem("Window/SceneCameraView/Select SceneViewCamera[1] #1")]
        public static void SelectSceneViewCamera1() => SelectSceneViewCamera(0);
        [MenuItem("Window/SceneCameraView/Select SceneViewCamera[2] #2")]
        public static void SelectSceneViewCamera2() => SelectSceneViewCamera(1);
        [MenuItem("Window/SceneCameraView/Select SceneViewCamera[3] #3")]
        public static void SelectSceneViewCamera3() => SelectSceneViewCamera(2);
        [MenuItem("Window/SceneCameraView/Select SceneViewCamera[4] #4")]
        public static void SelectSceneViewCamera4() => SelectSceneViewCamera(3);
        [MenuItem("Window/SceneCameraView/Select SceneViewCamera[5] #5")]
        public static void SelectSceneViewCamera5() => SelectSceneViewCamera(4);
        [MenuItem("Window/SceneCameraView/Select SceneViewCamera[6] #6")]
        public static void SelectSceneViewCamera6() => SelectSceneViewCamera(5);
        [MenuItem("Window/SceneCameraView/Select SceneViewCamera[7] #7")]
        public static void SelectSceneViewCamera7() => SelectSceneViewCamera(6);
        [MenuItem("Window/SceneCameraView/Select SceneViewCamera[8] #8")]
        public static void SelectSceneViewCamera8() => SelectSceneViewCamera(7);
        [MenuItem("Window/SceneCameraView/Select SceneViewCamera[9] #9")]
        public static void SelectSceneViewCamera9() => SelectSceneViewCamera(8);
        [MenuItem("Window/SceneCameraView/Select SceneViewCamera[0] #0")]
        public static void SelectSceneViewCamera0() => SelectSceneViewCamera(9);
        
        static void OnSceneGui(bool nextChecked)
        {
            _isInitialized = false;
            if (nextChecked)
            {
                SceneViewCameraMenuHolder.Save(true);
                SceneView.duringSceneGui += OnSceneGui;
            }
            else
            {
                SceneView.duringSceneGui -= OnSceneGui;
                SceneViewCameraMenuHolder.Save(false);
            }
            SceneView.RepaintAll();
        }
     
        static void OnSceneGui(SceneView sceneView)
        {
            _sceneView = sceneView;
            if (!_isInitialized)
            {
                _isInitialized = true;
                _windowRect.x = _sceneView.position.width - WINDOW_WIDTH;
                _windowRect.y = WINDOW_OFFSET_Y;
                SceneViewCameraBookmarkHolder.LoadAll();
            }
            _windowRect = GUILayout.Window(0, _windowRect, MovableWindow, "Scene View Camera");
            if (_windowRect.x < 0) _windowRect.x = 0;
            if (_windowRect.y < 0) _windowRect.y = 0;
            if (_windowRect.x+_windowRect.width > _sceneView.position.width) _windowRect.x = _sceneView.position.width - _windowRect.width;
            if (_windowRect.y+_windowRect.height> _sceneView.position.height) _windowRect.y = _sceneView.position.height - _windowRect.height;
            ShowButtons(sceneView.position.size);
        }

        static void MovableWindow(int windowId)
        {
            var sceneSkin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);

            Handles.BeginGUI();
            GUILayout.BeginVertical();

            if (_isInputMode)
            {
                var centeredStyle = new GUIStyle(sceneSkin.label);
                centeredStyle.alignment = TextAnchor.MiddleCenter;
                GUILayout.Label("- Input Mode -",centeredStyle);
                _inputModeOrthographic = GUILayout.Toggle(_inputModeOrthographic,"orthographic");
                _inputModePivot = EditorGUILayout.Vector3Field("pivot", _inputModePivot);
                Vector3 eulerAngles = _inputModeRotation.eulerAngles;
                eulerAngles = EditorGUILayout.Vector3Field("rotation", eulerAngles);
                _inputModeRotation = Quaternion.Euler(eulerAngles);
                _inputModeSize = EditorGUILayout.FloatField("size", _inputModeSize);
                _inputButtonGuiContent.text = "Set SceneCameraView";
                if (GUILayout.Button(_inputButtonGuiContent, sceneSkin.button))
                {
                    _sceneView.orthographic = _inputModeOrthographic;
                    _sceneView.pivot = _inputModePivot;
                    _sceneView.rotation = _inputModeRotation;
                    _sceneView.size = _inputModeSize;
                    _sceneView.ShowNotification(new GUIContent("Copied!"));
                }
                GUILayout.Space(20);
            }
            else
            {
                var fontStyle = new GUIStyle(sceneSkin.button);
                fontStyle.alignment = TextAnchor.MiddleLeft;
                _name = GUILayout.TextField(_name, 25);
                _orthoGuiContent.text = $"[ortho]: {_sceneView.orthographic}";
                _pivotGuiContent.text = $"[pivot]: {_sceneView.pivot}";
                _rotateGuiContent.text = $"[rotat]: {_sceneView.rotation}";
                _sizeGuiContent.text = $"[size]: {_sceneView.size}";
                if (GUILayout.Button(_orthoGuiContent, fontStyle))
                {
                    EditorGUIUtility.systemCopyBuffer = $"[ortho]: {_sceneView.orthographic}";
                    _sceneView.ShowNotification(new GUIContent("Copied!"));
                }

                if (GUILayout.Button(_pivotGuiContent, fontStyle))
                {
                    EditorGUIUtility.systemCopyBuffer = $"[pivot]: {_sceneView.pivot.ToString("F8")}";
                    _sceneView.ShowNotification(new GUIContent("Copied!"));
                }

                if (GUILayout.Button(_rotateGuiContent, fontStyle))
                {
                    EditorGUIUtility.systemCopyBuffer = $"[rotat]: {_sceneView.rotation.ToString("F8")}";
                    _sceneView.ShowNotification(new GUIContent("Copied!"));
                }

                if (GUILayout.Button(_sizeGuiContent, fontStyle))
                {
                    EditorGUIUtility.systemCopyBuffer = $"[size]: {_sceneView.size.ToString("F8")}";
                    _sceneView.ShowNotification(new GUIContent("Copied!"));
                }
                

                // PullDownMenu
                GUILayout.BeginHorizontal();
                _selectIndex = EditorGUILayout.Popup("", _selectIndex, _dropDownList,GUILayout.Width(30));
                if (GUILayout.Button($" - SAVE - ", sceneSkin.button))
                {
                    SceneViewCameraBookmarkHolder.Save(_selectIndex,new SceneViewCameraBookmark(_name, _sceneView));
                    _sceneView.ShowNotification(new GUIContent("Saved!"));
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(50);
            }
            
            EditorGUILayout.BeginHorizontal();

            _isInputMode = GUILayout.Toggle(_isInputMode,"InputMode");
            GUILayout.FlexibleSpace();
            var allDeleteStyle = new GUIStyle( sceneSkin.button );
            allDeleteStyle.normal.textColor = Color.red;
            if (GUILayout.Button($" - All Delete - ", allDeleteStyle, GUILayout.Width(80)))
            {
                _name = String.Empty;
                _selectIndex = 0;
                SceneViewCameraBookmarkHolder.AllRemove();
                _sceneView.ShowNotification(new GUIContent("All Deleted!"));
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.EndVertical();
            Handles.EndGUI();
            
            GUI.DragWindow();
        }
        
        /// <summary>
        /// ボタンの描画関数
        /// </summary>
        private static void ShowButtons(Vector2 sceneSize)
        {
            Handles.BeginGUI();
            var count = SceneViewCameraBookmarkHolder.BookmarkDic.Count;
            var buttonSize = 40;
            var deleteButtonSize = new Vector2(30,20);
            GUIContent content = new GUIContent();
            GUIContent deleteContent = new GUIContent();
            GUIStyle deleteStyle = new GUIStyle();

            deleteContent.text = "x";

            foreach (var item in SceneViewCameraBookmarkHolder.BookmarkDic.OrderBy(x=>x.Key).Select((Entry,Index)=> new {Entry,Index}))
            {
                if (item.Entry.Value == null)
                {
                    SceneViewCameraBookmarkHolder.Remove(item.Entry.Key);
                    continue;
                }
                
                // 画面下部、水平、中央寄せをコントロールする Rect
                var rect = new Rect(
                    sceneSize.x / 2 - buttonSize * count / 2 + buttonSize * item.Index,
                    sceneSize.y - buttonSize * 3f,
                    buttonSize,
                    buttonSize);

                content.text = _dropDownList[item.Entry.Key];
                content.tooltip = $"[name ] {item.Entry.Value._name}\n"+
                                  $"[ortho] {item.Entry.Value._orthographic}\n"+
                                  $"[pivot] {item.Entry.Value._pivot}\n"+
                                  $"[rotat] {item.Entry.Value._rotation}\n"+
                                  $"[size ] {item.Entry.Value._size}\n";
                if (GUI.Button(rect, content))
                {
                    SelectSceneViewCamera(item.Entry);
                }
                
                var oldColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.red;
                var deleteRect = new Rect(
                    sceneSize.x / 2 - buttonSize * count / 2 + buttonSize * item.Index + (buttonSize - deleteButtonSize.x) / 2,
                    sceneSize.y - buttonSize * 2f + 10,
                    deleteButtonSize.x,
                    deleteButtonSize.y);
                if (GUI.Button(deleteRect,deleteContent))
                {
                    SceneViewCameraBookmarkHolder.Remove(item.Entry.Key);
                }
                GUI.backgroundColor = oldColor;

            }
            Handles.EndGUI();
        }

        static void SelectSceneViewCamera(int index)
        {
            if (SceneViewCameraBookmarkHolder.BookmarkDic.TryGetValue(index, out var value))
            {
                var kvp = new KeyValuePair<int, SceneViewCameraBookmark>(index,value);
                SelectSceneViewCamera(kvp);
            }
        }

        static void SelectSceneViewCamera(KeyValuePair<int,SceneViewCameraBookmark> bookmark)
        {
            _name = bookmark.Value._name;
            _selectIndex = bookmark.Key;
            SceneView.lastActiveSceneView.MoveToBookmark(bookmark.Value);
        }
    }    
    static class SceneViewExtensions
    {
        public static void MoveToBookmark(this SceneView sceneView, SceneViewCameraBookmark sceneViewCameraBookmark)
        {
            if (null == sceneViewCameraBookmark)
            {
                return;
            }
            sceneView.orthographic = sceneViewCameraBookmark._orthographic;
            sceneView.pivot = sceneViewCameraBookmark._pivot;
            sceneView.rotation = sceneViewCameraBookmark._rotation;
            sceneView.size = sceneViewCameraBookmark._size;
        }
    }
}

