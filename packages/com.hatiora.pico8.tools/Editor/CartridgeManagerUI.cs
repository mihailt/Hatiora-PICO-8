using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hatiora.Pico8.Tools.Editor
{
    public class CartridgeManagerUI
    {
        private VisualElement _root;
        private Action _onCreateCartridge;
        private Action _onGenerateDocs;
        private Action _onGenerateTests;

        public string NewCartName
        {
            get => _newCartName?.value ?? "";
            set { if (_newCartName != null) _newCartName.value = value; }
        }

        public List<string> SourceFiles { get; private set; } = new List<string>();

        private TextField _newCartName;
        private ScrollView _cartList;
        private ScrollView _sourceFilesList;

        private VisualElement _colName;
        private VisualElement _colStatus;
        private VisualElement _colPort;
        private VisualElement _colDoc;
        private VisualElement _colActions;

        private VisualElement _cartridgesTabContent;
        private VisualElement _toolsTabContent;
        private VisualElement _wizardTabContent;

        private List<Button> _tabButtons = new List<Button>();

        public CartridgeManagerUI(VisualElement root, Action onCreateCartridge, Action onGenerateDocs, Action onGenerateTests)
        {
            _root = root;
            _onCreateCartridge = onCreateCartridge;
            _onGenerateDocs = onGenerateDocs;
            _onGenerateTests = onGenerateTests;

            BuildUI();
        }

        private void BuildUI()
        {
            _root.style.paddingLeft = 10;
            _root.style.paddingRight = 10;
            _root.style.paddingTop = 10;
            _root.style.paddingBottom = 10;

            var title = new Label("PICO-8 Tools");
            title.style.fontSize = 20;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 15;
            _root.Add(title);

            // Tab Bar
            var tabBar = new VisualElement();
            tabBar.style.flexDirection = FlexDirection.Row;
            tabBar.style.marginBottom = 15;
            tabBar.style.borderBottomWidth = 1;
            tabBar.style.borderBottomColor = Color.gray;

            var cartTabBtn = CreateTabButton("Cartridges", () => ShowTab(_cartridgesTabContent, 0));
            var toolsTabBtn = CreateTabButton("Tools", () => ShowTab(_toolsTabContent, 1));
            var wizardTabBtn = CreateTabButton("Wizard", () => ShowTab(_wizardTabContent, 2));

            tabBar.Add(cartTabBtn);
            tabBar.Add(toolsTabBtn);
            tabBar.Add(wizardTabBtn);
            _root.Add(tabBar);

            // Containers
            var contentContainer = new VisualElement();
            contentContainer.style.flexGrow = 1;
            _root.Add(contentContainer);

            _cartridgesTabContent = BuildCartridgesTab();
            _toolsTabContent = BuildToolsTab();
            _wizardTabContent = BuildWizardTab();

            contentContainer.Add(_cartridgesTabContent);
            contentContainer.Add(_toolsTabContent);
            contentContainer.Add(_wizardTabContent);

            // Init Default Tab
            ShowTab(_cartridgesTabContent, 0);
        }

        private Button CreateTabButton(string text, Action onClick)
        {
            var btn = new Button(onClick) { text = text };
            btn.style.flexGrow = 1;
            btn.style.paddingTop = 8;
            btn.style.paddingBottom = 8;
            btn.style.borderBottomLeftRadius = 0;
            btn.style.borderBottomRightRadius = 0;
            btn.style.borderTopWidth = 1;
            btn.style.borderLeftWidth = 1;
            btn.style.borderRightWidth = 1;
            btn.style.borderBottomWidth = 0;
            _tabButtons.Add(btn);
            return btn;
        }

        private void ShowTab(VisualElement activeContent, int activeIndex)
        {
            _cartridgesTabContent.style.display = DisplayStyle.None;
            _toolsTabContent.style.display = DisplayStyle.None;
            _wizardTabContent.style.display = DisplayStyle.None;

            activeContent.style.display = DisplayStyle.Flex;

            for (int i = 0; i < _tabButtons.Count; i++)
            {
                if (i == activeIndex)
                {
                    _tabButtons[i].style.backgroundColor = new Color(0.35f, 0.35f, 0.35f);
                    _tabButtons[i].style.unityFontStyleAndWeight = FontStyle.Bold;
                }
                else
                {
                    _tabButtons[i].style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
                    _tabButtons[i].style.unityFontStyleAndWeight = FontStyle.Normal;
                }
            }
        }

        private VisualElement BuildCartridgesTab()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;

            var listTitle = new Label("Installed Cartridges");
            listTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            listTitle.style.fontSize = 14;
            listTitle.style.marginBottom = 10;
            container.Add(listTitle);

            _cartList = new ScrollView();
            _cartList.style.flexGrow = 1;
            _cartList.style.marginBottom = 20;
            _cartList.style.paddingTop = 5;
            _cartList.style.paddingBottom = 5;
            _cartList.style.borderTopWidth = 1;
            _cartList.style.borderBottomWidth = 1;
            _cartList.style.borderTopColor = Color.gray;
            _cartList.style.borderBottomColor = Color.gray;
            container.Add(_cartList);

            var columnsContainer = new VisualElement();
            columnsContainer.style.flexDirection = FlexDirection.Row;
            columnsContainer.style.flexGrow = 1;
            _cartList.Add(columnsContainer);

            _colName = CreateColumn(120);
            _colStatus = CreateColumn(100);
            _colPort = CreateColumn(100);
            _colDoc = CreateColumn(100);
            _colActions = CreateColumn(0, 1);

            columnsContainer.Add(_colName);
            columnsContainer.Add(_colStatus);
            columnsContainer.Add(_colPort);
            columnsContainer.Add(_colDoc);
            columnsContainer.Add(_colActions);

            RefreshCartridgesList();
            return container;
        }

        private VisualElement CreateColumn(float width, float flexGrow = 0)
        {
            var col = new VisualElement();
            col.style.flexDirection = FlexDirection.Column;
            if (width > 0) col.style.width = width;
            if (flexGrow > 0) col.style.flexGrow = flexGrow;
            return col;
        }

        private VisualElement BuildToolsTab()
        {
            var container = new VisualElement();
            
            var globalBox = new Box();
            globalBox.style.paddingTop = 15;
            globalBox.style.paddingBottom = 15;
            globalBox.style.paddingLeft = 15;
            globalBox.style.paddingRight = 15;
            
            var globalTitle = new Label("Global Helpers");
            globalTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            globalTitle.style.marginBottom = 15;
            globalTitle.style.fontSize = 14;
            globalBox.Add(globalTitle);

            var globalRow = new VisualElement();
            globalRow.style.flexDirection = FlexDirection.Row;
            
            var genAllDocsBtn = new Button(_onGenerateDocs) { text = "Generate All Docs" };
            genAllDocsBtn.style.flexGrow = 1;
            genAllDocsBtn.style.height = 30;
            
            var genAllTestsBtn = new Button(_onGenerateTests) { text = "Generate All Tests" };
            genAllTestsBtn.style.flexGrow = 1;
            genAllTestsBtn.style.height = 30;

            var reimportTexturesBtn = new Button(ReimportAllTextures) { text = "Reimport All Textures" };
            reimportTexturesBtn.style.flexGrow = 1;
            reimportTexturesBtn.style.height = 30;
            
            globalRow.Add(genAllDocsBtn);
            globalRow.Add(genAllTestsBtn);
            globalRow.Add(reimportTexturesBtn);
            globalBox.Add(globalRow);
            container.Add(globalBox);

            return container;
        }

        private VisualElement BuildWizardTab()
        {
            var container = new VisualElement();

            var createBox = new Box();
            createBox.style.paddingTop = 15;
            createBox.style.paddingBottom = 15;
            createBox.style.paddingLeft = 15;
            createBox.style.paddingRight = 15;

            var createTitle = new Label("Create New Cartridge Package");
            createTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            createTitle.style.fontSize = 14;
            createTitle.style.marginBottom = 15;
            createBox.Add(createTitle);

            var nameRow = new VisualElement();
            nameRow.style.flexDirection = FlexDirection.Row;
            nameRow.style.alignItems = Align.Center;

            var prefixLabel = new Label("com.hatiora.pico8.");
            nameRow.Add(prefixLabel);

            _newCartName = new TextField();
            _newCartName.style.flexGrow = 1;
            nameRow.Add(_newCartName);
            createBox.Add(nameRow);

            var sourceBox = new Box();
            sourceBox.style.marginTop = 15;
            sourceBox.style.paddingTop = 10;
            sourceBox.style.paddingBottom = 10;
            sourceBox.style.paddingLeft = 10;
            sourceBox.style.paddingRight = 10;

            var sourceTitleRow = new VisualElement();
            sourceTitleRow.style.flexDirection = FlexDirection.Row;
            sourceTitleRow.style.alignItems = Align.Center;
            sourceTitleRow.style.marginBottom = 10;
            
            var sourceTitle = new Label("Source Files (.p8 / .lua)");
            sourceTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            sourceTitle.style.flexGrow = 1;

            var browseBtn = new Button(BrowseSourceFile) { text = "Add File" };
            
            sourceTitleRow.Add(sourceTitle);
            sourceTitleRow.Add(browseBtn);
            sourceBox.Add(sourceTitleRow);

            _sourceFilesList = new ScrollView();
            _sourceFilesList.style.minHeight = 50;
            _sourceFilesList.style.maxHeight = 150;
            _sourceFilesList.style.borderTopWidth = 1;
            _sourceFilesList.style.borderTopColor = Color.gray;
            _sourceFilesList.style.paddingTop = 10;
            sourceBox.Add(_sourceFilesList);
            
            createBox.Add(sourceBox);
            RefreshSourceFilesList();

            var createBtn = new Button(_onCreateCartridge) { text = "Create Cartridge" };
            createBtn.style.marginTop = 15;
            createBtn.style.height = 30;
            createBox.Add(createBtn);

            container.Add(createBox);
            return container;
        }

        public void RefreshCartridgesList()
        {
            if (_colName == null) return;
            
            _colName.Clear();
            _colStatus.Clear();
            _colPort.Clear();
            _colDoc.Clear();
            _colActions.Clear();

            _colName.Add(CreateHeaderCell("Cartridge"));
            _colStatus.Add(CreateHeaderCell("Status"));
            _colPort.Add(CreateHeaderCell("Port"));
            _colDoc.Add(CreateHeaderCell("Doc"));
            _colActions.Add(CreateHeaderCell("Actions"));

            var carts = CartridgeService.GetInstalledCartridges();
            int rowIndex = 0;
            foreach (var cart in carts)
            {
                var bgColor = (rowIndex % 2 == 0) ? new Color(0.2f, 0.2f, 0.2f, 0.5f) : Color.clear;

                var lbl = new Label(cart.rawName);
                _colName.Add(CreateDataCell(lbl, bgColor));

                var statusDropdown = new EnumField(cart.status);
                statusDropdown.RegisterValueChangedCallback(evt => 
                {
                    CartridgeService.SetCartridgeFlag(cart.fullPkgName, "hatiora_status", evt.newValue.ToString());
                });
                _colStatus.Add(CreateDataCell(statusDropdown, bgColor));
                
                var portDropdown = new EnumField(cart.portStatus);
                portDropdown.RegisterValueChangedCallback(evt => 
                {
                    CartridgeService.SetCartridgeFlag(cart.fullPkgName, "hatiora_portStatus", evt.newValue.ToString());
                });
                _colPort.Add(CreateDataCell(portDropdown, bgColor));

                var docDropdown = new EnumField(cart.docStatus);
                docDropdown.RegisterValueChangedCallback(evt => 
                {
                    CartridgeService.SetCartridgeFlag(cart.fullPkgName, "hatiora_docStatus", evt.newValue.ToString());
                });
                _colDoc.Add(CreateDataCell(docDropdown, bgColor));

                var btnsGroup = new VisualElement();
                btnsGroup.style.flexDirection = FlexDirection.Row;
                btnsGroup.style.justifyContent = Justify.FlexStart;

                var extBtn = new Button(() => P8Extractor.Extract(Path.Combine(Application.dataPath, "../../..", cart.refsRelDir), Path.Combine(Application.dataPath, "../../..", "packages", cart.fullPkgName, "Runtime", "Resources"), cart.rawName, cart.fileFilter)) { text = "Extract" };
                var docBtn = new Button(() => CartridgeDocGenerator.Generate(cart.rawName, cart.fullPkgName, cart.refsRelDir, cart.fileFilter)) { text = "Gen Docs" };
                var testBtn = new Button(() => CartridgeTestGenerator.Generate(cart.rawName, cart.fullPkgName)) { text = "Gen Tests" };
                
                foreach (var btn in new[] { extBtn, docBtn, testBtn })
                {
                    btn.style.width = 75;
                }

                btnsGroup.Add(extBtn);
                btnsGroup.Add(docBtn);
                btnsGroup.Add(testBtn);

                _colActions.Add(CreateDataCell(btnsGroup, bgColor));

                rowIndex++;
            }
        }

        public void RefreshSourceFilesList()
        {
            if (_sourceFilesList == null) return;

            _sourceFilesList.Clear();
            if (SourceFiles.Count == 0)
            {
                var lbl = new Label("No source files selected. Will default to Hello template.");
                lbl.style.color = Color.gray;
                lbl.style.unityFontStyleAndWeight = FontStyle.Italic;
                _sourceFilesList.Add(lbl);
                return;
            }

            for (int i = 0; i < SourceFiles.Count; i++)
            {
                int index = i;
                string path = SourceFiles[index];
                
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 2;

                var lbl = new Label(Path.GetFileName(path));
                lbl.style.flexGrow = 1;
                lbl.tooltip = path;
                
                var removeBtn = new Button(() => 
                {
                    SourceFiles.RemoveAt(index);
                    RefreshSourceFilesList();
                }) { text = "X" };

                row.Add(lbl);
                row.Add(removeBtn);
                _sourceFilesList.Add(row);
            }
        }

        private void BrowseSourceFile()
        {
            string path = EditorUtility.OpenFilePanel("Select Source File", "", "p8,lua");
            if (!string.IsNullOrEmpty(path) && !SourceFiles.Contains(path))
            {
                SourceFiles.Add(path);
                
                if (string.IsNullOrEmpty(_newCartName.value))
                {
                    string fileName = Path.GetFileNameWithoutExtension(path);
                    _newCartName.value = fileName.ToLower();
                }
                
                RefreshSourceFilesList();
            }
        }

        private static void ReimportAllTextures()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D");
            int reimportedCount = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("Pico-8/packages") || path.Contains("pico8") || path.ToLower().Contains("gfx") || path.ToLower().Contains("label"))
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                    reimportedCount++;
                }
            }
            Debug.Log($"[PICO-8 Tools] Forced reimport of {reimportedCount} PICO-8 textures.");
        }

        private VisualElement CreateHeaderCell(string text)
        {
            var cell = new VisualElement();
            cell.style.height = 30;
            cell.style.justifyContent = Justify.Center;
            cell.style.paddingLeft = 5;
            cell.style.paddingRight = 5;
            cell.style.borderBottomWidth = 1;
            cell.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
            
            var label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            cell.Add(label);
            
            return cell;
        }

        private VisualElement CreateDataCell(VisualElement content, Color bgColor)
        {
            var cell = new VisualElement();
            cell.style.height = 36;
            cell.style.justifyContent = Justify.Center;
            cell.style.paddingLeft = 5;
            cell.style.paddingRight = 5;
            cell.style.backgroundColor = bgColor;
            
            if (content != null)
            {
                cell.Add(content);
            }
            return cell;
        }
    }
}
