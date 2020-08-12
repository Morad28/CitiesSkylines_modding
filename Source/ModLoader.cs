using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using PrefabHook;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CustomizedAI
{
    public class ModLoader : LoadingExtensionBase
    {
        

        // Counting time





        // Called when the level is created
        public override void OnCreated(ILoading loading)
        {


            base.OnCreated(loading);

            StreamWriter sw = new StreamWriter(@"C:\Users\adeye\Desktop\Output1.txt", true);
            sw.WriteLine("New simulation started");
            sw.Close();
            // register event handlers
            VehicleInfoHook.OnPreInitialization += OnPreVehicleInit;
            VehicleInfoHook.OnPostInitialization += OnPostVehicleInit;

            // deploy (after event handler registration!)
            VehicleInfoHook.Deploy();


           
        }

        // Called when the level is loaded
        public override void OnLevelLoaded(LoadMode mode)
        {

            // Writing 
            #region
            StreamWriter sw = new StreamWriter(@"C:\Users\adeye\Desktop\Output1.txt", true);

            sw.Close();

            #endregion

            // UI DESIGN HERE: All that comes is only to design UI
            #region UI
            #region Collect Data from one vehicle button

            // Follow vehicle checkbox: to collect data for all vehicules
            CitizenVehicleWorldInfoPanel panel = UIView.library.Get<CitizenVehicleWorldInfoPanel>(typeof(CitizenVehicleWorldInfoPanel).Name);
            UICheckBox checkBox = panel.component.AddUIComponent<UICheckBox>();
            panel.component.height = 321f + 35f + 16f;
            createCheckBox(checkBox, new Vector3(22f, 2f), new Vector3(14f, 164f + 130f + 5f), "Follow");
            checkBox.width = panel.component.width;

            // Doesn't work
            CityServiceVehicleWorldInfoPanel panel1 = UIView.library.Get<CityServiceVehicleWorldInfoPanel>(typeof(CityServiceVehicleWorldInfoPanel).Name);
            UICheckBox checkBox1 = panel1.component.AddUIComponent<UICheckBox>();
            panel1.component.height = 321f + 5f + 16f;
            createCheckBox(checkBox1, new Vector3(22f, 2f), new Vector3(14f, 164f + 130f + 5f), "Follow");
            checkBox.width = panel1.component.width;

            // Doesn't work
            PublicTransportVehicleWorldInfoPanel panel2 = UIView.library.Get<PublicTransportVehicleWorldInfoPanel>(typeof(PublicTransportVehicleWorldInfoPanel).Name);
            UICheckBox checkBox2 = panel2.component.AddUIComponent<UICheckBox>();
            panel2.component.height = 321f + 5f + 16f;
            createCheckBox(checkBox2, new Vector3(22f, 2f), new Vector3(14f, 164f + 130f + 5f), "Follow");
            checkBox.width = panel2.component.width;

            #endregion

            #region Collect Data from all vehicles button
            CitizenVehicleWorldInfoPanel panelB = UIView.library.Get<CitizenVehicleWorldInfoPanel>(typeof(CitizenVehicleWorldInfoPanel).Name);
            UICheckBox checkBoxB = panelB.component.AddUIComponent<UICheckBox>();
            checkBoxB.width = panelB.component.width;
            createCheckBox(checkBoxB, new Vector3(22f, 2f), new Vector3(14f, 164f + 160f + 5f), "Follow All");
            panelB.component.height = 321f + 35f + 16f;

            #endregion

            #region only vehicles button
            CitizenVehicleWorldInfoPanel panelG = UIView.library.Get<CitizenVehicleWorldInfoPanel>(typeof(CitizenVehicleWorldInfoPanel).Name);
            UICheckBox checkBoxOV = panelG.component.AddUIComponent<UICheckBox>();

            checkBoxOV.width = panelG.component.width;
            checkBoxOV.height = 20f;
            checkBoxOV.clipChildren = true;

            UISprite spriteOV = checkBoxOV.AddUIComponent<UISprite>();
            spriteOV.spriteName = "ToggleBase";
            spriteOV.size = new Vector2(16f, 16f);
            spriteOV.relativePosition = Vector3.zero;

            checkBoxOV.checkedBoxObject = spriteOV.AddUIComponent<UISprite>();
            ((UISprite)checkBoxOV.checkedBoxObject).spriteName = "ToggleBaseFocused";
            checkBoxOV.checkedBoxObject.size = new Vector2(16f, 16f);
            checkBoxOV.checkedBoxObject.relativePosition = Vector3.zero;

            checkBoxOV.label = checkBoxOV.AddUIComponent<UILabel>();
            checkBoxOV.label.text = " ";
            checkBoxOV.label.textScale = 0.9f;
            checkBoxOV.label.relativePosition = new Vector3(22f, 2f);

            checkBoxOV.name = "No pedestrian";
            checkBoxOV.text = "No pedestrian";

            checkBoxOV.relativePosition = new Vector3(14f + 90f, 164f + 130f + 5f);

            panelG.component.height = 321f + 35f + 16f;

            #endregion

            #region only pedestrian button
            CitizenVehicleWorldInfoPanel panelH = UIView.library.Get<CitizenVehicleWorldInfoPanel>(typeof(CitizenVehicleWorldInfoPanel).Name);
            UICheckBox checkBoxOP = panelH.component.AddUIComponent<UICheckBox>();

            checkBoxOP.width = panelH.component.width;
            checkBoxOP.height = 20f;
            checkBoxOP.clipChildren = true;

            UISprite spriteOP = checkBoxOP.AddUIComponent<UISprite>();
            spriteOP.spriteName = "ToggleBase";
            spriteOP.size = new Vector2(16f, 16f);
            spriteOP.relativePosition = Vector3.zero;

            checkBoxOP.checkedBoxObject = spriteOP.AddUIComponent<UISprite>();
            ((UISprite)checkBoxOP.checkedBoxObject).spriteName = "ToggleBaseFocused";
            checkBoxOP.checkedBoxObject.size = new Vector2(16f, 16f);
            checkBoxOP.checkedBoxObject.relativePosition = Vector3.zero;

            checkBoxOP.label = checkBoxOP.AddUIComponent<UILabel>();
            checkBoxOP.label.text = " ";
            checkBoxOP.label.textScale = 0.9f;
            checkBoxOP.label.relativePosition = new Vector3(22f, 2f);

            checkBoxOP.name = "No vehicles";
            checkBoxOP.text = "No vehicles";

            checkBoxOP.relativePosition = new Vector3(14f + 120f - 30f, 164f + 160f + 5f);

            panelH.component.height = 321f + 35f + 16f;

            #endregion

            // Creating the inputs (text fields)
            // Opening angle
            #region
            CitizenVehicleWorldInfoPanel panelC = UIView.library.Get<CitizenVehicleWorldInfoPanel>(typeof(CitizenVehicleWorldInfoPanel).Name);
            UITextField openingAngleui = panelC.component.AddUIComponent<UITextField>();
            openingAngleui.width = 1;
            openingAngleui.cursorWidth = 1;
            openingAngleui.cursorBlinkTime = 0.45f;
            openingAngleui.area = new Vector4(156, 4, 60, 25);
            openingAngleui.bottomColor = new Color32(254, 254, 254, 255);
            openingAngleui.bringTooltipToFront = true;
            openingAngleui.builtinKeyNavigation = true;
            openingAngleui.cachedName = "param1";
            openingAngleui.clipChildren = false;
            openingAngleui.color = new Color32(110, 110, 110, 225);
            openingAngleui.colorizeSprites = false;
            openingAngleui.disabledColor = new Color32(254, 254, 254, 255);
            openingAngleui.disabledTextColor = new Color32(254, 254, 254, 255);
            openingAngleui.dropShadowColor = new Color32(0, 0, 0, 0);
            openingAngleui.dropShadowOffset = new Vector2(0, 0);
            openingAngleui.enabled = true;
            openingAngleui.focusedBgSprite = "OptionsDropboxListboxHovered";
            openingAngleui.isInteractive = true;
            openingAngleui.numericalOnly = true;
            openingAngleui.normalBgSprite = "OptionsDropboxListboxHovered";
            openingAngleui.normalFgSprite = "OptionsDropboxListboxHovered";
            openingAngleui.opacity = 1;
            openingAngleui.outlineColor = new Color32(0, 0, 0, 255);
            openingAngleui.outlineSize = 1;
            openingAngleui.padding = new RectOffset(7, 7, 4, 4);
            openingAngleui.selectionBackgroundColor = new Color32(0, 171, 234, 255);
            openingAngleui.size = new Vector2(60, 25);
            openingAngleui.color = new Color32(174, 197, 211, 255);
            openingAngleui.textScale = 1.125f;
            openingAngleui.useGUILayout = true;
            openingAngleui.zOrder = 1;
            openingAngleui.text = (Mathf.Acos(CustomAI.VangleFront) * 180 / Mathf.PI).ToString();
            openingAngleui.allowFloats = true;
            openingAngleui.relativePosition = new Vector3(120f, 164f + 20f + 5f);
            panelC.component.height = 321f + 35f + 16f;
            #endregion
            // Forward angle
            #region
            CitizenVehicleWorldInfoPanel panelD = UIView.library.Get<CitizenVehicleWorldInfoPanel>(typeof(CitizenVehicleWorldInfoPanel).Name);
            UITextField fwdAngleui;
            fwdAngleui = panelD.component.AddUIComponent<UITextField>();
            fwdAngleui.width = 1;
            fwdAngleui.cursorWidth = 1;
            fwdAngleui.cursorBlinkTime = 0.45f;
            fwdAngleui.area = new Vector4(156, 4, 60, 25);
            fwdAngleui.bottomColor = new Color32(254, 254, 254, 255);
            fwdAngleui.bringTooltipToFront = true;
            fwdAngleui.builtinKeyNavigation = true;
            fwdAngleui.cachedName = "param2";
            fwdAngleui.clipChildren = false;
            fwdAngleui.color = new Color32(110, 110, 110, 225);
            fwdAngleui.colorizeSprites = false;
            fwdAngleui.disabledColor = new Color32(254, 254, 254, 255);
            fwdAngleui.disabledTextColor = new Color32(254, 254, 254, 255);
            fwdAngleui.dropShadowColor = new Color32(0, 0, 0, 0);
            fwdAngleui.dropShadowOffset = new Vector2(0, 0);
            fwdAngleui.enabled = true;
            fwdAngleui.focusedBgSprite = "OptionsDropboxListboxHovered";
            fwdAngleui.isInteractive = true;
            fwdAngleui.numericalOnly = true;
            fwdAngleui.normalBgSprite = "OptionsDropboxListboxHovered";
            fwdAngleui.normalFgSprite = "OptionsDropboxListboxHovered";
            fwdAngleui.opacity = 1;
            fwdAngleui.outlineColor = new Color32(0, 0, 0, 255);
            fwdAngleui.outlineSize = 1;
            fwdAngleui.padding = new RectOffset(7, 7, 4, 4);
            fwdAngleui.selectionBackgroundColor = new Color32(0, 171, 234, 255);
            fwdAngleui.size = new Vector2(60, 25);
            fwdAngleui.color = new Color32(174, 197, 211, 255);
            fwdAngleui.textScale = 1.125f;
            fwdAngleui.useGUILayout = true;
            fwdAngleui.zOrder = 1;
            fwdAngleui.allowFloats = true;
            panelD.component.height = 321f + 35f + 16f;
            fwdAngleui.text = (Mathf.Acos(CustomAI.angleMoveFWD) * 180 / Mathf.PI).ToString();
            fwdAngleui.relativePosition = new Vector3(120f, 164f + 90f + 5f);
            #endregion
            // Backward angle
            #region
            UITextField bwdangleui;
            CitizenVehicleWorldInfoPanel panelE = UIView.library.Get<CitizenVehicleWorldInfoPanel>(typeof(CitizenVehicleWorldInfoPanel).Name);
            bwdangleui = panelE.component.AddUIComponent<UITextField>();
            bwdangleui.cursorWidth = 1;
            bwdangleui.cursorBlinkTime = 0.45f;
            bwdangleui.area = new Vector4(156, 4, 60, 25);
            bwdangleui.bottomColor = new Color32(254, 254, 254, 255);
            bwdangleui.bringTooltipToFront = true;
            bwdangleui.builtinKeyNavigation = true;
            bwdangleui.cachedName = "param3";
            bwdangleui.clipChildren = false;
            bwdangleui.color = new Color32(110, 110, 110, 225);
            bwdangleui.colorizeSprites = false;
            bwdangleui.disabledColor = new Color32(254, 254, 254, 255);
            bwdangleui.disabledTextColor = new Color32(254, 254, 254, 255);
            bwdangleui.dropShadowColor = new Color32(0, 0, 0, 0);
            bwdangleui.dropShadowOffset = new Vector2(0, 0);
            bwdangleui.enabled = true;
            bwdangleui.focusedBgSprite = "OptionsDropboxListboxHovered";
            bwdangleui.isInteractive = true;
            bwdangleui.numericalOnly = true;
            bwdangleui.normalBgSprite = "OptionsDropboxListboxHovered";
            bwdangleui.normalFgSprite = "OptionsDropboxListboxHovered";
            bwdangleui.opacity = 1;
            bwdangleui.outlineColor = new Color32(0, 0, 0, 255);
            bwdangleui.outlineSize = 1;
            bwdangleui.padding = new RectOffset(7, 7, 4, 4);
            bwdangleui.selectionBackgroundColor = new Color32(0, 171, 234, 255);
            bwdangleui.size = new Vector2(60, 25);
            bwdangleui.color = new Color32(174, 197, 211, 255);
            bwdangleui.textScale = 1.125f;
            bwdangleui.useGUILayout = true;
            bwdangleui.zOrder = 1;
            bwdangleui.allowFloats = true;
            panelE.component.height = 321f + 35f + 16f;
            bwdangleui.text = (Mathf.Acos(-CustomAI.angleMoveBWD) * 180 / Mathf.PI).ToString();
            bwdangleui.relativePosition = new Vector3(300f, 164f + 30f + 5f);
            #endregion
            // Distance
            #region
            UITextField distanceui;
            CitizenVehicleWorldInfoPanel panelF = UIView.library.Get<CitizenVehicleWorldInfoPanel>(typeof(CitizenVehicleWorldInfoPanel).Name);
            distanceui = panelF.component.AddUIComponent<UITextField>();
            distanceui.cursorWidth = 1;
            distanceui.cursorBlinkTime = 0.45f;
            distanceui.area = new Vector4(156, 4, 60, 25);
            distanceui.bottomColor = new Color32(254, 254, 254, 255);
            distanceui.bringTooltipToFront = true;
            distanceui.builtinKeyNavigation = true;
            distanceui.cachedName = "param4";
            distanceui.clipChildren = false;
            distanceui.color = new Color32(110, 110, 110, 225);
            distanceui.colorizeSprites = false;
            distanceui.disabledColor = new Color32(254, 254, 254, 255);
            distanceui.disabledTextColor = new Color32(254, 254, 254, 255);
            distanceui.dropShadowColor = new Color32(0, 0, 0, 0);
            distanceui.dropShadowOffset = new Vector2(0, 0);
            distanceui.enabled = true;
            distanceui.focusedBgSprite = "OptionsDropboxListboxHovered";
            distanceui.isInteractive = true;
            distanceui.numericalOnly = true;
            distanceui.normalBgSprite = "OptionsDropboxListboxHovered";
            distanceui.normalFgSprite = "OptionsDropboxListboxHovered";
            distanceui.opacity = 1;
            distanceui.outlineColor = new Color32(0, 0, 0, 255);
            distanceui.outlineSize = 1;
            distanceui.padding = new RectOffset(7, 7, 4, 4);
            distanceui.selectionBackgroundColor = new Color32(0, 171, 234, 255);
            distanceui.size = new Vector2(60, 25);
            distanceui.color = new Color32(174, 197, 211, 255);
            distanceui.textScale = 1.125f;
            distanceui.useGUILayout = true;
            distanceui.zOrder = 1;
            distanceui.allowFloats = true;
            panelF.component.height = 321f + 35f + 16f;
            distanceui.text = CustomAI.distanceMagnitude.ToString();
            distanceui.relativePosition = new Vector3(300f, 164f + 90f + 5f);
            #endregion
            // Labels 
            #region
            UILabel txtlabelFA = panelC.component.AddUIComponent<UILabel>();
            txtlabelFA.text = "Opening angle";
            txtlabelFA.relativePosition = new Vector3(0f, 164f + 20f + 5f);

            UILabel txtmoveFWD = panelC.component.AddUIComponent<UILabel>();
            txtmoveFWD.text = "Angle moving fwd";
            txtmoveFWD.relativePosition = new Vector3(0f, 164f + 60f + 5f);

            UILabel txtmoveBWD = panelC.component.AddUIComponent<UILabel>();
            txtmoveBWD.text = "Angle moving bwd";
            txtmoveBWD.relativePosition = new Vector3(250f, 164f + 20f + 5f);

            UILabel distance = panelC.component.AddUIComponent<UILabel>();
            distance.text = "Distance";
            distance.relativePosition = new Vector3(250f, 164f + 60f + 5f);
            #endregion

            // Events handler
            // Called When the checkbox is ticked 
            checkBox.eventCheckChanged += (component, check) =>
            {
                ushort VehicleID = WorldInfoPanel.GetCurrentInstanceID().Vehicle;

                if (check)
                {
                    CustomAI.VID = VehicleID;
                    Debug.Log("Changed to " + VehicleID);
                }
                else
                {

                    Debug.Log("Value set to default");
                }
            };

            checkBoxB.eventCheckChanged += (component, check) =>
            {
                ushort VehicleID = WorldInfoPanel.GetCurrentInstanceID().Vehicle;

                if (check)
                {
                    CustomAI.AllVehicles = true;
                    Debug.Log("Collecting all vehicles data...");
                }
                else
                {
                    CustomAI.AllVehicles = false;
                    Debug.Log("Collecting vehicle with ID: " + VehicleID);
                }
            };

            checkBoxOV.eventCheckChanged += CheckBoxOV_eventCheckChanged;

            checkBoxOP.eventCheckChanged += CheckBoxOP_eventCheckChanged;
            // Called when text changed (when writing)
            openingAngleui.eventTextChanged += Text_eventTextChanged;

            fwdAngleui.eventTextChanged += FwdAngleui_eventTextChanged;

            bwdangleui.eventTextChanged += Bwdangleui_eventTextChanged;

            distance.eventTextChanged += Distance_eventTextChanged;

            #endregion UI


            // Renaming for better referencment
            #region
            // Added to the custom AI  
            PrefabCollection<VehicleInfo>.FindLoaded("773594206.MAZDA CX-5_Data").name = "Car";
            

            PrefabCollection<VehicleInfo>.FindLoaded("658859533.Scania Citywide LFDD_Data").name = "Bus";
            

            VehicleInfo Businfo2 = PrefabCollection<VehicleInfo>.FindLoaded("Bus");
            

            PrefabCollection<VehicleInfo>.FindLoaded("Oil Truck").name = "Truck" ;

            // Keeped original AI
            

            //PrefabCollection<VehicleInfo>.FindLoaded("Ore Truck").name = "Truck";

            //PrefabCollection<VehicleInfo>.FindLoaded("875955591.Container truck EU - KALMAR_Data").name = "Truck";

            //PrefabCollection<VehicleInfo>.FindLoaded("Station-wagon").name = "Car";

            //PrefabCollection<VehicleInfo>.FindLoaded("Electric Car 04").name = "Car";

            //PrefabCollection<VehicleInfo>.FindLoaded("665226973.Tesla Model 3_Data").name = "Car";

            //PrefabCollection<VehicleInfo>.FindLoaded("474665067.Ford Focus AD update_Data").name = "Car";

            //PrefabCollection<VehicleInfo>.FindLoaded("757326158.Volvo XC90_Data").name = "Car";

            //PrefabCollection<VehicleInfo>.FindLoaded("Jeep").name = "Car";

            //PrefabCollection<VehicleInfo>.FindLoaded("Suv").name = "Car";

            //PrefabCollection<VehicleInfo>.FindLoaded("Personal Electric Transport Ride").name = "Car";

            //PrefabCollection<VehicleInfo>.FindLoaded("Camper Van 01").name = "Car";

            //PrefabCollection<VehicleInfo>.FindLoaded("625698592.Volvo S60 II Police (SWE)_Data POLICE").name = "Police";

            #endregion

           

            
           



        }

        private void CheckBoxOP_eventCheckChanged(UIComponent component, bool check)
        {
            if (check)
            {
                CustomAI.onlyVehicles = false;
                Debug.Log("Only pedestrians mode");
            }
            else
            {
                CustomAI.onlyVehicles = true;
                Debug.Log("Only pedestrians mode disabled");
            }
        }

        private void CheckBoxOV_eventCheckChanged(UIComponent component, bool check)
        {
            if (check)
            {
                CustomAI.onlyPedestrian = false;
                Debug.Log("Only vehicles mode");
            }
            else
            {
                CustomAI.onlyPedestrian = true;
                Debug.Log("Only vehicles mode disabled");
            }
        }

         private void createCheckBox(UICheckBox ckb, Vector3 relativPosLabel, Vector3 relativPos, string txt)
        {
            ckb.isVisible = true;
            ckb.height = 20f;
            ckb.clipChildren = true;

            UISprite spriteB = ckb.AddUIComponent<UISprite>();
            spriteB.spriteName = "ToggleBase";
            spriteB.size = new Vector2(16f, 16f);
            spriteB.relativePosition = Vector3.zero;

            ckb.checkedBoxObject = spriteB.AddUIComponent<UISprite>();
            ((UISprite)ckb.checkedBoxObject).spriteName = "ToggleBaseFocused";
            ckb.checkedBoxObject.size = new Vector2(16f, 16f);
            ckb.checkedBoxObject.relativePosition = Vector3.zero;

            ckb.label = ckb.AddUIComponent<UILabel>();
            ckb.label.text = " ";
            ckb.label.textScale = 0.9f;
            ckb.label.relativePosition = relativPosLabel;

            ckb.text = txt;

            ckb.relativePosition = relativPos ;
        }

        private void Distance_eventTextChanged(UIComponent component, string value)
        {
            float flt1 = float.Parse(value);
            CustomAI.distanceMagnitude = flt1;
            Debug.Log(CustomAI.distanceMagnitude);
        }

        private void Bwdangleui_eventTextChanged(UIComponent component, string value)
        {
            
            float flt1 =Mathf.Cos((float.Parse(value)-180) * Mathf.PI / 180);
            CustomAI.angleMoveBWD = flt1;
            Debug.Log("Setting to " + flt1);
            
        }

        private void FwdAngleui_eventTextChanged(UIComponent component, string value)
        {
            float flt1 = Mathf.Cos(float.Parse(value) * Mathf.PI / 180);
            CustomAI.angleMoveFWD = flt1;
            Debug.Log("Setting to " + Mathf.Cos(float.Parse(value) * Mathf.PI / 180));
            
        }

        private void Text_eventTextChanged(UIComponent component, string value)
        {
           
            float flt1 = Mathf.Cos(float.Parse(value) * Mathf.PI / 180);
            CustomAI.VangleFront = flt1;
            Debug.Log("Setting to " + Mathf.Cos(float.Parse(value) * Mathf.PI / 180));
           
        }

        public override void OnReleased()
        {
            base.OnReleased();

            if (!IsHooked()) return;

            // revert on release
            VehicleInfoHook.Revert();
        }


        // This event handler is called before vehicle initialization
        public void OnPreVehicleInit(VehicleInfo info)
        {
            // AI Modification here!
            // Car
            // Mazda
            info = PrefabCollection<VehicleInfo>.FindLoaded("773594206.MAZDA CX-5_Data"); // Here you look for an already existing car
            var oldAI = info.GetComponent<PassengerCarAI>();   // removing the current AI of the car
            UnityEngine.Object.DestroyImmediate(oldAI);  // Destroying the AI attached to the game  
            // Adding the new AI to the gameobject
            var newAIinfo = info.gameObject.AddComponent<CustomAI>(); 
            newAIinfo.m_info = info; 
            info.m_vehicleAI = newAIinfo;

            // Bus
            info = PrefabCollection<VehicleInfo>.FindLoaded("658859533.Scania Citywide LFDD_Data");
            var busoldAI = info.GetComponent<BusAI>();
            UnityEngine.Object.DestroyImmediate(busoldAI);
            var newbusAIinfo = info.gameObject.AddComponent<CustomAI>();
            newbusAIinfo.m_info = info;
            info.m_vehicleAI = newbusAIinfo;

            info = PrefabCollection<VehicleInfo>.FindLoaded("Bus");
            var busoldAI2 = info.GetComponent<BusAI>();
            UnityEngine.Object.DestroyImmediate(busoldAI2);
            var newbusAIinfo2 = info.gameObject.AddComponent<CustomAI>();
            newbusAIinfo2.m_info = info;
            info.m_vehicleAI = newbusAIinfo2;

            // Trucks 
            info = PrefabCollection<VehicleInfo>.FindLoaded("Oil Truck");
            var truckOldAI = info.GetComponent<CargoTruckAI>();
            UnityEngine.Object.DestroyImmediate(truckOldAI);
            var newTruckAIInfo = info.gameObject.AddComponent<CustomAI>();
            newTruckAIInfo.m_info = info;
            info.m_vehicleAI = newTruckAIInfo;

            info = PrefabCollection<VehicleInfo>.FindLoaded("875955591.Container truck EU - KALMAR_Data");
            var truckOldAI2 = info.GetComponent<CargoTruckAI>();
            UnityEngine.Object.DestroyImmediate(truckOldAI2);
            var newTruckAIInfo2 = info.gameObject.AddComponent<CustomAI>();
            newTruckAIInfo2.m_info = info;
            info.m_vehicleAI = newTruckAIInfo2;

            UnityEngine.Debug.Log("Game is now initializing VehicleInfo " + info.name);
        }

        // This event handler is called after vehicle initialization
        public void OnPostVehicleInit(VehicleInfo info)
        {
            // your code here
            Debug.Log("Game has initialized VehicleInfo " + info.name);
        }


        // checks if the player subscribed to the Prefab Hook mod
        private bool IsHooked()
        {
            foreach (PluginManager.PluginInfo current in PluginManager.instance.GetPluginsInfo())
            {
                if (current.publishedFileID.AsUInt64 == 530771650uL) return true;
            }
            return false;
        }

        public override void OnLevelUnloading()
        {
            StreamWriter sw = new StreamWriter(@"C:\Users\adeye\Desktop\Output1.txt", true);
            sw.WriteLine("Simulation time," + SimulationManager.instance.m_simulationTimer2);
            sw.Close();
        }


    }
    
}