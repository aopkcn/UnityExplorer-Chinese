using UnityExplorer.UI.Panels;
using UniverseLib.UI;
using UniverseLib.UI.Models;

namespace UnityExplorer.UI.Widgets
{
    public class GameObjectInfoPanel
    {
        public GameObjectControls Owner { get; }
        GameObject Target => Owner.Target;

        string lastGoName;
        string lastPath;
        bool lastParentState;
        int lastSceneHandle;
        string lastTag;
        int lastLayer;
        int lastFlags;

        ButtonRef ViewParentButton;
        InputFieldRef PathInput;

        InputFieldRef NameInput;
        Toggle ActiveSelfToggle;
        Text ActiveSelfText;
        Toggle IsStaticToggle;

        ButtonRef SceneButton;

        InputFieldRef InstanceIDInput;
        InputFieldRef TagInput;

        Dropdown LayerDropdown;
        Dropdown FlagsDropdown;

        public GameObjectInfoPanel(GameObjectControls owner)
        {
            this.Owner = owner;
            Create();
        }

        public void UpdateGameObjectInfo(bool firstUpdate, bool force)
        {
            if (firstUpdate)
            {
                InstanceIDInput.Text = Target.GetInstanceID().ToString();
            }

            if (force || (!NameInput.Component.isFocused && Target.name != lastGoName))
            {
                lastGoName = Target.name;
                Owner.Parent.Tab.TabText.text = $"[G] {Target.name}";
                NameInput.Text = Target.name;
            }

            if (force || !PathInput.Component.isFocused)
            {
                string path = Target.transform.GetTransformPath();
                if (path != lastPath)
                {
                    lastPath = path;
                    PathInput.Text = path;
                }
            }

            if (force || Target.transform.parent != lastParentState)
            {
                lastParentState = Target.transform.parent;
                ViewParentButton.Component.interactable = lastParentState;
                if (lastParentState)
                {
                    ViewParentButton.ButtonText.color = Color.white;
                    ViewParentButton.ButtonText.text = "◄ 查看父项";
                }
                else
                {
                    ViewParentButton.ButtonText.color = Color.grey;
                    ViewParentButton.ButtonText.text = "没有父项";
                }
            }

            if (force || Target.activeSelf != ActiveSelfToggle.isOn)
            {
                ActiveSelfToggle.Set(Target.activeSelf, false);
                ActiveSelfText.color = ActiveSelfToggle.isOn ? Color.green : Color.red;
            }

            if (force || Target.isStatic != IsStaticToggle.isOn)
            {
                IsStaticToggle.Set(Target.isStatic, false);
            }

            if (force || Target.scene.handle != lastSceneHandle)
            {
                lastSceneHandle = Target.scene.handle;
                SceneButton.ButtonText.text = Target.scene.IsValid() ? Target.scene.name : "无 (资产/资源)";
            }

            if (force || (!TagInput.Component.isFocused && Target.tag != lastTag))
            {
                lastTag = Target.tag;
                TagInput.Text = lastTag;
            }

            if (force || (Target.layer != lastLayer))
            {
                lastLayer = Target.layer;
                LayerDropdown.value = Target.layer;
            }

            if (force || ((int)Target.hideFlags != lastFlags))
            {
                lastFlags = (int)Target.hideFlags;
                FlagsDropdown.captionText.text = Target.hideFlags.ToString();
            }
        }

        void DoSetParent(Transform transform)
        {
            ExplorerCore.Log($"将目标的转换父对象设置为: {(transform == null ? "null" : $"'{transform.name}'")}");

            if (Target.GetComponent<RectTransform>())
                Target.transform.SetParent(transform, false);
            else
                Target.transform.parent = transform;

            UpdateGameObjectInfo(false, false);

            Owner.TransformControl.UpdateTransformControlValues(false);
        }


        #region UI event listeners

        void OnViewParentClicked()
        {
            if (this.Target && this.Target.transform.parent)
            {
                Owner.Parent.OnTransformCellClicked(this.Target.transform.parent.gameObject);
            }
        }

        void OnPathEndEdit(string input)
        {
            lastPath = input;

            if (string.IsNullOrEmpty(input))
            {
                DoSetParent(null);
            }
            else
            {
                Transform parentToSet = null;

                if (input.EndsWith("/"))
                    input = input.Remove(input.Length - 1);

                // try the easy way
                if (GameObject.Find(input) is GameObject found)
                {
                    parentToSet = found.transform;
                }
                else
                {
                    // look for inactive objects
                    string name = input.Split('/').Last();
                    UnityEngine.Object[] allObjects = RuntimeHelper.FindObjectsOfTypeAll(typeof(GameObject));
                    List<GameObject> shortList = new();
                    foreach (UnityEngine.Object obj in allObjects)
                        if (obj.name == name) shortList.Add(obj.TryCast<GameObject>());
                    foreach (GameObject go in shortList)
                    {
                        string path = go.transform.GetTransformPath(true);
                        if (path.EndsWith("/"))
                            path = path.Remove(path.Length - 1);
                        if (path == input)
                        {
                            parentToSet = go.transform;
                            break;
                        }
                    }
                }

                if (parentToSet)
                    DoSetParent(parentToSet);
                else
                {
                    ExplorerCore.LogWarning($"找不到任何游戏对象名称或路径 '{input}'!");
                    UpdateGameObjectInfo(false, true);
                }
            }

        }

        void OnNameEndEdit(string value)
        {
            Target.name = value;
            UpdateGameObjectInfo(false, true);
        }

        void OnCopyClicked()
        {
            ClipboardPanel.Copy(this.Target);
        }

        void OnActiveSelfToggled(bool value)
        {
            Target.SetActive(value);
            UpdateGameObjectInfo(false, true);
        }

        void OnTagEndEdit(string value)
        {
            try
            {
                Target.tag = value;
                UpdateGameObjectInfo(false, true);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"异常设置标签! {ex.ReflectionExToString()}");
            }
        }
        
        void OnSceneButtonClicked()
        {
            InspectorManager.Inspect(Target.scene);
        }

        void OnExploreButtonClicked()
        {
            ObjectExplorerPanel panel = UIManager.GetPanel<ObjectExplorerPanel>(UIManager.Panels.ObjectExplorer);
            panel.SceneExplorer.JumpToTransform(this.Owner.Parent.Target.transform);
        }

        void OnLayerDropdownChanged(int value)
        {
            Target.layer = value;
            UpdateGameObjectInfo(false, true);
        }

        void OnFlagsDropdownChanged(int value)
        {
            try
            {
                HideFlags enumVal = hideFlagsValues[FlagsDropdown.options[value].text];
                Target.hideFlags = enumVal;

                UpdateGameObjectInfo(false, true);
            }
            catch (Exception ex)
            {
                ExplorerCore.LogWarning($"异常设置隐藏标志: {ex}");
            }
        }

        void OnDestroyClicked()
        {
            GameObject.Destroy(this.Target);
            InspectorManager.ReleaseInspector(Owner.Parent);
        }

        void OnInstantiateClicked()
        {
            GameObject clone = GameObject.Instantiate(this.Target);
            InspectorManager.Inspect(clone);
        }

        #endregion


        #region UI Construction

        public void Create()
        {
            GameObject topInfoHolder = UIFactory.CreateVerticalGroup(Owner.Parent.Content, "TopInfoHolder", false, false, true, true, 3,
                new Vector4(3, 3, 3, 3), new Color(0.1f, 0.1f, 0.1f), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(topInfoHolder, minHeight: 100, flexibleWidth: 9999);
            topInfoHolder.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // first row (parent, path)

            GameObject firstRow = UIFactory.CreateUIObject("ParentRow", topInfoHolder);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(firstRow, false, false, true, true, 5, 0, 0, 0, 0, default);
            UIFactory.SetLayoutElement(firstRow, minHeight: 25, flexibleWidth: 9999);

            ViewParentButton = UIFactory.CreateButton(firstRow, "ViewParentButton", "◄ 查看父项", new Color(0.2f, 0.2f, 0.2f));
            ViewParentButton.ButtonText.fontSize = 13;
            UIFactory.SetLayoutElement(ViewParentButton.Component.gameObject, minHeight: 25, minWidth: 100);
            ViewParentButton.OnClick += OnViewParentClicked;

            this.PathInput = UIFactory.CreateInputField(firstRow, "PathInput", "...");
            PathInput.Component.textComponent.color = Color.grey;
            PathInput.Component.textComponent.fontSize = 14;
            UIFactory.SetLayoutElement(PathInput.UIRoot, minHeight: 25, minWidth: 100, flexibleWidth: 9999);
            PathInput.Component.lineType = InputField.LineType.MultiLineSubmit;

            ButtonRef copyButton = UIFactory.CreateButton(firstRow, "CopyButton", "复制到剪贴板", new Color(0.2f, 0.2f, 0.2f, 1));
            copyButton.ButtonText.color = Color.yellow;
            UIFactory.SetLayoutElement(copyButton.Component.gameObject, minHeight: 25, minWidth: 120);
            copyButton.OnClick += OnCopyClicked;

            PathInput.Component.GetOnEndEdit().AddListener((string val) => { OnPathEndEdit(val); });

            // Title and update row

            GameObject titleRow = UIFactory.CreateUIObject("TitleRow", topInfoHolder);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(titleRow, false, false, true, true, 5);

            Text titleLabel = UIFactory.CreateLabel(titleRow, "Title", SignatureHighlighter.Parse(typeof(GameObject), false),
                TextAnchor.MiddleLeft, fontSize: 17);
            UIFactory.SetLayoutElement(titleLabel.gameObject, minHeight: 30, minWidth: 100);

            // name

            NameInput = UIFactory.CreateInputField(titleRow, "NameInput", "无标题");
            UIFactory.SetLayoutElement(NameInput.Component.gameObject, minHeight: 30, minWidth: 100, flexibleWidth: 9999);
            NameInput.Component.textComponent.fontSize = 15;
            NameInput.Component.GetOnEndEdit().AddListener((string val) => { OnNameEndEdit(val); });

            // second row (toggles, instanceID, tag, buttons)

            GameObject secondRow = UIFactory.CreateUIObject("ParentRow", topInfoHolder);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(secondRow, false, false, true, true, 5, 0, 0, 0, 0, default);
            UIFactory.SetLayoutElement(secondRow, minHeight: 25, flexibleWidth: 9999);

            // activeSelf
            GameObject activeToggleObj = UIFactory.CreateToggle(secondRow, "ActiveSelf", out ActiveSelfToggle, out ActiveSelfText);
            UIFactory.SetLayoutElement(activeToggleObj, minHeight: 25, minWidth: 100);
            ActiveSelfText.text = "ActiveSelf";
            ActiveSelfToggle.onValueChanged.AddListener(OnActiveSelfToggled);

            // isStatic
            GameObject isStaticObj = UIFactory.CreateToggle(secondRow, "IsStatic", out IsStaticToggle, out Text staticText);
            UIFactory.SetLayoutElement(isStaticObj, minHeight: 25, minWidth: 80);
            staticText.text = "IsStatic";
            staticText.color = Color.grey;
            IsStaticToggle.interactable = false;

            // InstanceID
            Text instanceIdLabel = UIFactory.CreateLabel(secondRow, "InstanceIDLabel", "实例 ID:", TextAnchor.MiddleRight, Color.grey);
            UIFactory.SetLayoutElement(instanceIdLabel.gameObject, minHeight: 25, minWidth: 90);

            InstanceIDInput = UIFactory.CreateInputField(secondRow, "InstanceIDInput", "错误");
            UIFactory.SetLayoutElement(InstanceIDInput.Component.gameObject, minHeight: 25, minWidth: 110);
            InstanceIDInput.Component.textComponent.color = Color.grey;
            InstanceIDInput.Component.readOnly = true;

            //Tag
            Text tagLabel = UIFactory.CreateLabel(secondRow, "TagLabel", "标签:", TextAnchor.MiddleRight, Color.grey);
            UIFactory.SetLayoutElement(tagLabel.gameObject, minHeight: 25, minWidth: 40);

            TagInput = UIFactory.CreateInputField(secondRow, "TagInput", "无");
            UIFactory.SetLayoutElement(TagInput.Component.gameObject, minHeight: 25, minWidth: 100, flexibleWidth: 999);
            TagInput.Component.textComponent.color = Color.white;
            TagInput.Component.GetOnEndEdit().AddListener((string val) => { OnTagEndEdit(val); });

            // Instantiate
            ButtonRef instantiateBtn = UIFactory.CreateButton(secondRow, "InstantiateBtn", "实例化", new Color(0.2f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(instantiateBtn.Component.gameObject, minHeight: 25, minWidth: 120);
            instantiateBtn.OnClick += OnInstantiateClicked;

            // Destroy
            ButtonRef destroyBtn = UIFactory.CreateButton(secondRow, "DestroyBtn", "销毁", new Color(0.3f, 0.2f, 0.2f));
            UIFactory.SetLayoutElement(destroyBtn.Component.gameObject, minHeight: 25, minWidth: 80);
            destroyBtn.OnClick += OnDestroyClicked;

            // third row (scene, layer, flags)

            GameObject thirdrow = UIFactory.CreateUIObject("ParentRow", topInfoHolder);
            UIFactory.SetLayoutGroup<HorizontalLayoutGroup>(thirdrow, false, false, true, true, 5, 0, 0, 0, 0, default);
            UIFactory.SetLayoutElement(thirdrow, minHeight: 25, flexibleWidth: 9999);

            // Inspect in Explorer button
            ButtonRef explorerBtn = UIFactory.CreateButton(thirdrow, "ExploreBtn", "在资源管理器里显示", new Color(0.15f, 0.15f, 0.15f));
            UIFactory.SetLayoutElement(explorerBtn.Component.gameObject, minHeight: 25, minWidth: 100);
            explorerBtn.ButtonText.fontSize = 12;
            explorerBtn.OnClick += OnExploreButtonClicked;

            // Scene
            Text sceneLabel = UIFactory.CreateLabel(thirdrow, "SceneLabel", "场景:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(sceneLabel.gameObject, minHeight: 25, minWidth: 50);

            SceneButton = UIFactory.CreateButton(thirdrow, "SceneButton", "无标题");
            UIFactory.SetLayoutElement(SceneButton.Component.gameObject, minHeight: 25, minWidth: 120, flexibleWidth: 999);
            SceneButton.OnClick += OnSceneButtonClicked;

            // Layer
            Text layerLabel = UIFactory.CreateLabel(thirdrow, "LayerLabel", "层:", TextAnchor.MiddleLeft, Color.grey);
            UIFactory.SetLayoutElement(layerLabel.gameObject, minHeight: 25, minWidth: 50);

            GameObject layerDrop = UIFactory.CreateDropdown(thirdrow, "LayerDropdown", out LayerDropdown, "0", 14, OnLayerDropdownChanged);
            UIFactory.SetLayoutElement(layerDrop, minHeight: 25, minWidth: 110, flexibleWidth: 999);
            LayerDropdown.captionText.color = SignatureHighlighter.EnumGreen;
            if (layerToNames == null)
                GetLayerNames();
            foreach (string name in layerToNames)
                LayerDropdown.options.Add(new Dropdown.OptionData(name));
            LayerDropdown.value = 0;
            LayerDropdown.RefreshShownValue();

            // Flags
            Text flagsLabel = UIFactory.CreateLabel(thirdrow, "FlagsLabel", "标志:", TextAnchor.MiddleRight, Color.grey);
            UIFactory.SetLayoutElement(flagsLabel.gameObject, minHeight: 25, minWidth: 50);

            GameObject flagsDrop = UIFactory.CreateDropdown(thirdrow, "FlagsDropdown", out FlagsDropdown, "None", 14, OnFlagsDropdownChanged);
            FlagsDropdown.captionText.color = SignatureHighlighter.EnumGreen;
            UIFactory.SetLayoutElement(flagsDrop, minHeight: 25, minWidth: 135, flexibleWidth: 999);
            if (hideFlagsValues == null)
                GetHideFlagNames();
            foreach (string name in hideFlagsValues.Keys)
                FlagsDropdown.options.Add(new Dropdown.OptionData(name));
            FlagsDropdown.value = 0;
            FlagsDropdown.RefreshShownValue();
        }

        private static List<string> layerToNames;

        private static void GetLayerNames()
        {
            layerToNames = new List<string>();
            for (int i = 0; i < 32; i++)
            {
                string name = RuntimeHelper.LayerToName(i);
                if (string.IsNullOrEmpty(name))
                    name = i.ToString();
                layerToNames.Add(name);
            }
        }

        private static Dictionary<string, HideFlags> hideFlagsValues;

        private static void GetHideFlagNames()
        {
            hideFlagsValues = new Dictionary<string, HideFlags>();

            Array names = Enum.GetValues(typeof(HideFlags));
            foreach (HideFlags value in names)
            {
                hideFlagsValues.Add(value.ToString(), value);
            }
        }

        #endregion
   
    }
}
