using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PastryWorld.Editor
{
    /// <summary>
    /// W0 Day2：程序化创建 PastryWorldInput.inputactions。
    /// 3 Action Map（Player/Craft/UI）+ 9 Action + 3 控制方案。
    /// 一次性脚本，运行后可删除。
    /// </summary>
    public static class CreateInputActions
    {
        const string k_KBM = "KeyboardMouse";
        const string k_Touch = "Touchscreen";
        const string k_Pad = "Gamepad";

        [MenuItem("Tools/W0/创建 PastryWorldInput.inputactions")]
        public static void Run()
        {
            const string dir = "Assets/Settings/Input";
            const string path = dir + "/PastryWorldInput.inputactions";
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/Settings", "Input");

            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            asset.name = "PastryWorldInput";

            BuildPlayerMap(asset);
            BuildCraftMap(asset);
            BuildUIMap(asset);

            // 3 个控制方案
            asset.AddControlScheme(new InputControlScheme(k_KBM).WithDevice("<Keyboard>", true).WithDevice("<Mouse>", false));
            asset.AddControlScheme(new InputControlScheme(k_Touch).WithDevice("<Touchscreen>", true));
            asset.AddControlScheme(new InputControlScheme(k_Pad).WithDevice("<Gamepad>", true));

            var json = asset.ToJson();
            System.IO.File.WriteAllText(path, json);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            Debug.Log($"[CreateInputActions] 已创建：{path}（{asset.actionMaps.Count} maps, {asset.controlSchemes.Count} schemes, {json.Length} bytes）");
        }

        static void BuildPlayerMap(InputActionAsset asset)
        {
            var map = asset.AddActionMap("Player");

            var move = map.AddAction("Move", InputActionType.Value);
            move.expectedControlType = "Vector2";
            Add2DVector(move, "<Keyboard>/w", "<Keyboard>/s", "<Keyboard>/a", "<Keyboard>/d", k_KBM);
            Add2DVector(move, "<Keyboard>/upArrow", "<Keyboard>/downArrow", "<Keyboard>/leftArrow", "<Keyboard>/rightArrow", k_KBM);
            move.AddBinding("<Gamepad>/leftStick", groups: k_Pad);

            var interact = map.AddAction("Interact", InputActionType.Button);
            interact.AddBinding("<Keyboard>/e", groups: k_KBM);
            interact.AddBinding("<Keyboard>/space", groups: k_KBM);
            interact.AddBinding("<Gamepad>/buttonSouth", groups: k_Pad);
            interact.AddBinding("<Touchscreen>/tap", groups: k_Touch);

            var cancel = map.AddAction("Cancel", InputActionType.Button);
            cancel.AddBinding("<Keyboard>/escape", groups: k_KBM);
            cancel.AddBinding("<Gamepad>/buttonEast", groups: k_Pad);
        }

        static void BuildCraftMap(InputActionAsset asset)
        {
            var map = asset.AddActionMap("Craft");

            var pointer = map.AddAction("Pointer", InputActionType.Value);
            pointer.expectedControlType = "Vector2";
            pointer.AddBinding("<Mouse>/position", groups: k_KBM);
            pointer.AddBinding("<Touchscreen>/position", groups: k_Touch);

            var press = map.AddAction("Press", InputActionType.Button);
            press.AddBinding("<Mouse>/leftButton", groups: k_KBM);
            press.AddBinding("<Touchscreen>/press", groups: k_Touch);

            var drag = map.AddAction("Drag", InputActionType.PassThrough);
            drag.expectedControlType = "Vector2";
            drag.AddBinding("<Mouse>/delta", groups: k_KBM);
            drag.AddBinding("<Touchscreen>/delta", groups: k_Touch);
        }

        static void BuildUIMap(InputActionAsset asset)
        {
            var map = asset.AddActionMap("UI");

            var navigate = map.AddAction("Navigate", InputActionType.Value);
            navigate.expectedControlType = "Vector2";
            Add2DVector(navigate, "<Keyboard>/upArrow", "<Keyboard>/downArrow", "<Keyboard>/leftArrow", "<Keyboard>/rightArrow", k_KBM);
            navigate.AddBinding("<Gamepad>/dpad", groups: k_Pad);

            var submit = map.AddAction("Submit", InputActionType.Button);
            submit.AddBinding("<Keyboard>/enter", groups: k_KBM);
            submit.AddBinding("<Gamepad>/buttonSouth", groups: k_Pad);

            var cancel = map.AddAction("Cancel", InputActionType.Button);
            cancel.AddBinding("<Keyboard>/escape", groups: k_KBM);
            cancel.AddBinding("<Gamepad>/buttonEast", groups: k_Pad);
        }

        static void Add2DVector(InputAction action,
            string up, string down, string left, string right, string groups)
        {
            action.AddCompositeBinding("2DVector")
                .With("Up", up, groups: groups)
                .With("Down", down, groups: groups)
                .With("Left", left, groups: groups)
                .With("Right", right, groups: groups);
        }
    }
}
