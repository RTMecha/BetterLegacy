using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.UI;

using BetterLegacy.Core;
using BetterLegacy.Core.Data;
using BetterLegacy.Editor.Data;
using LSFunctions;
using BetterLegacy.Core.Helpers;
using BetterLegacy.Core.Managers;
using UnityEngine.EventSystems;
using TMPro;
using System.IO;
using BetterLegacy.Core.Prefabs;
using BetterLegacy.Editor.Data.Dialogs;
using BetterLegacy.Editor.Data.Popups;

namespace BetterLegacy.Editor.Managers
{
    public class EditorDocumentation : MonoBehaviour
    {
        #region Init

        public static EditorDocumentation inst;

        public static void Init() => Creator.NewGameObject(nameof(EditorDocumentation), EditorManager.inst.transform.parent).AddComponent<EditorDocumentation>();

        void Awake()
        {
            inst = this;
            GenerateUI();
        }

        void GenerateUI() // NEED TO UPDATE!
        {
            RTEditor.inst.DocumentationPopup = RTEditor.inst.GeneratePopup(EditorPopup.DOCUMENTATION_POPUP, "Documentation", Vector2.zero, new Vector2(600f, 450f), _val =>
            {
                documentationSearch = _val;
                RefreshDocumentation();
            }, placeholderText: "Search for document...");

            EditorHelper.AddEditorDropdown("Wiki / Documentation", "", "Help", SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{RTFile.BepInExAssetsPath}editor_gui_question.png"), () =>
            {
                RTEditor.inst.DocumentationPopup.Open();
                RefreshDocumentation();
            });

            var editorDialogObject = EditorPrefabHolder.Instance.Dialog.Duplicate(EditorManager.inst.dialogs, "DocumentationDialog");
            editorDialogObject.transform.AsRT().anchoredPosition = new Vector2(0f, 16f);
            editorDialogObject.transform.AsRT().sizeDelta = new Vector2(0f, 32f);
            var dialogStorage = editorDialogObject.GetComponent<EditorDialogStorage>();

            dialogStorage.topPanel.color = LSColors.HexToColor("D89356");
            dialogStorage.title.text = "- Documentation -";
            documentationTitle = dialogStorage.title;

            var editorDialogSpacer = editorDialogObject.transform.GetChild(1);
            editorDialogSpacer.AsRT().sizeDelta = new Vector2(765f, 54f);

            Destroy(editorDialogObject.transform.GetChild(2).gameObject);

            EditorHelper.AddEditorDialog(EditorDialog.DOCUMENTATION, editorDialogObject);

            var scrollView = EditorPrefabHolder.Instance.ScrollView.Duplicate(editorDialogObject.transform, "Scroll View");
            documentationContent = scrollView.transform.Find("Viewport/Content");

            var scrollViewLE = scrollView.AddComponent<LayoutElement>();
            scrollViewLE.ignoreLayout = true;

            scrollView.transform.AsRT().anchoredPosition = new Vector2(392.5f, 320f);
            scrollView.transform.AsRT().sizeDelta = new Vector2(735f, 638f);

            EditorThemeManager.AddGraphic(editorDialogObject.GetComponent<Image>(), ThemeGroup.Background_1);

            documentationContent.GetComponent<VerticalLayoutGroup>().spacing = 12f;

            GenerateDocument("Introduction", "Welcome to Project Arrhythmia Legacy.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("Welcome to <b>Project Arrhythmia</b>!\nWhether you're new to the game, modding or have been around for a while, I'm sure this " +
                        "documentation will help massively in understanding the ins and outs of the editor and the game as a whole.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("These documents only list editor features and how to use them, everything else is listed in the Github wiki.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>DOCUMENTATION INFO</b>", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> represents a feature from original Legacy, with very minor tweaks done to it if any.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[MODDED]</b> represents a feature added by BetterLegacy. These features will not work in unmodded PA.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[PATCHED]</b> represents a feature modified by BetterLegacy. They're either in newer versions of PA or are partially modded, meaning they might not work in regular PA.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[ALPHA]</b> represents a feature ported from alpha to BetterLegacy and are not present in Legacy.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[LEGACY]</b> represents a Legacy feature that was discontinued in alpha.", EditorDocument.Element.Type.Text)
            });

            GenerateDocument("Credits", "All the people who helped the mod development in some way.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("Reimnop's Catalyst (PA object and animation optimization)", EditorDocument.Element.Type.Text),
                LinkElement("<b>Source code</b>:\nhttps://github.com/Reimnop/Catalyst", "https://github.com/Reimnop/Catalyst"),

                new EditorDocument.Element("Reimnop's ILMath (fast math parser / evaluator)", EditorDocument.Element.Type.Text),
                LinkElement("<b>Source code</b>:\nhttps://github.com/Reimnop/ILMath", "https://github.com/Reimnop/ILMath"),

                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Keijiro Takahashi's KinoGlitch (AnalogGlitch and DigitalGlitch events)", EditorDocument.Element.Type.Text),
                LinkElement("<b>Source code</b>:\nhttps://github.com/keijiro/KinoGlitch", "https://github.com/keijiro/KinoGlitch"),

                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("WestHillApps' UniBpmAnalyzer", EditorDocument.Element.Type.Text),
                LinkElement("<b>Source code</b>:\nhttps://github.com/WestHillApps/UniBpmAnalyzer", "https://github.com/WestHillApps/UniBpmAnalyzer"),

                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Nick Vogt's ColliderCreator (used for creating proper collision for the custom shapes)", EditorDocument.Element.Type.Text),
                LinkElement("<b>Website</b>:\nhttps://www.h3xed.com/", "https://www.h3xed.com/"),
                LinkElement("<b>Source code</b>:\nhttps://www.h3xed.com/programming/automatically-create-polygon-collider-2d-from-2d-mesh-in-unity", "https://www.h3xed.com/programming/automatically-create-polygon-collider-2d-from-2d-mesh-in-unity"),

                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Crafty Font for the Pixellet font.", EditorDocument.Element.Type.Text),
                LinkElement("<b>Website</b>:\nhttps://craftyfont.gumroad.com/", "https://craftyfont.gumroad.com/"),

                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("HAWTPIXEL for the File Deletion font.", EditorDocument.Element.Type.Text),
                LinkElement("<b>Website</b>:\nhttps://www.hawtpixel.com/", "https://www.hawtpixel.com/"),

                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Sans Sans font.", EditorDocument.Element.Type.Text),
                LinkElement("<b>Website</b>:\nhttps://www.font.download/font/sans", "https://www.font.download/font/sans"),

                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Fontworks for the RocknRoll font.", EditorDocument.Element.Type.Text),
                LinkElement("<b>Website</b>:\nhttps://github.com/fontworks-fonts/RocknRoll", "https://github.com/fontworks-fonts/RocknRoll"),

                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("ManiackersDesign for the Monomaniac One font.", EditorDocument.Element.Type.Text),
                LinkElement("<b>Website</b>:\nhttps://github.com/ManiackersDesign/monomaniac", "https://github.com/ManiackersDesign/monomaniac"),

                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>SPECIAL THANKS</b>", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Pidge (developer of the game) - Obviously for making the game itself and inspiring some features in BetterLegacy.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("enchart - Massively helped RTMecha get into modding in the first place. Without enchart, none of this would have been possible.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("aiden_ytarame - Ported gradient objects from alpha to BetterLegacy.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("SleepyzGamer - Helped a lot in finding things", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("KarasuTori - For motivating RTMecha to keep going and experimenting with modding.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("MoNsTeR and CubeCube for testing the mods, reporting bugs and giving suggestions.", EditorDocument.Element.Type.Text),
            });

            GenerateDocument("Uploading a Level", "Guidelines on uploading levels.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("Want to post a level you made somewhere? Well, make sure you read these guidelines first.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("If you made a level in BetterLegacy <i>with</i> modded features or the song is not verified*, you <b>cannot</b> upload it to the Steam Workshop. You can only share it through the Arcade server or some other means.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Some modded features (as specified in the Introductions section) are more likely to break vanilla PA than others, but just to be safe: continue with the above point.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Even if you add warnings to your level where you state that it is modded, it doesn't matter because most people don't read titles / descriptions and will probably end up blaming the game for being broken, even if it really isn't.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("If you made a level in BetterLegacy <i>without</i> modded features and the song is verified, then you can upload it to the Steam Workshop. But you <b>MUST</b> verify that the level works in vanilla first by playtesting it all the way through in both vanilla Alpha and vanilla Legacy.\n\n" +
                                    "- If you want to playtest in Alpha, you can click the Convert to VG format button in Upload > Publish / Update Level, navigating to beatmaps/exports and moving the exported level to the beatmaps/editor folder where your Project Arrhythmia folder is.\n" +
                                    "- If you want to playtest in Legacy, you can just copy the level to your beatmaps/editor folder where your Project Arrhythmia folder is.\n" +
                                    "- Some features like object gradients, ColorGrading (hue only), Gradient event, Player Force event, and Prefab Transforms (position, scale, rotation) can be used in alpha, but it's still recommended to playtest in vanilla Alpha anyways.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("* Non-verified songs are allowed on the Arcade server because the Arcade server is not officially endorsed by Vitamin Games nor the Steam Workshop, much like how some other music-based games have an unofficial server for hosting levels. If a song is super-protected, however, it has a high chance of being taken down regardless of that fact.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("When uploading to the Arcade server, you can include tags to better define your level. When uploading a level, you have to keep these tag guidelines in mind:\n" +
                                    "- Tags are best with underscores replacing spaces, e.g. <b>\"boss_level\"</b>.\n" +
                                    "- Uppercase and lowercase is acceptable.\n" +
                                    "- Keep tags relevant to the level.\n" +
                                    "- Include series name. If the level is not a part of a series, then skip this.\n" +
                                    "- Include any important characters involved, e.g. <b>\"para\"</b>. If there are no characters, then skip this.\n" +
                                    "- Include the general theme of the level, e.g. <b>\"spooky\"</b>.\n" +
                                    "- If your level covers subject matter most people will find uncomfortable, PLEASE include it in the tags so people know to avoid (e.g. gore (<i>looking at you KarasuTori</i>)). If you do not follow this, then the level will be removed." +
                                    "- Add <b>\"joke_level\"</b> or <b>\"meme\"</b> to tags if your level isn't serious one. Can be both tags.\n" +
                                    "- If your level has the possibility to cause an epileptic seizure, include <b>\"flashing_lights\"</b>." +
                                    "- If your level features specific functions (like Window movement, video BG, etc), please tag that as well.\n" +
                                    "- Add <b>\"high_detail\"</b> to the tags if the level has a ton of objects / is lagging.\n" +
                                    "- Add <b>\"reupload\"</b> or <b>\"remake\"</b> to the tags if your level is such.\n" +
                                    "- Unfinished levels can be uploaded, but will need the <b>\"wip\"</b> tag. (do not flood the server with these)\n" +
                                    "- Older levels can be uploaded, but will need the <b>\"old\"</b> tag.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<i>Do not</i> reupload other people's levels to the server unless you have their permission. If you do reupload, it has to have been a level from the Steam workshop.\n" +
                                    "Remakes are allowed, as long as you have permission from the original creator(s).", EditorDocument.Element.Type.Text),
                LinkElement("If you want to comment on a level, join the System Error Discord server (<color=#0084FF>https://discord.gg/nB27X2JZcY</color>), go to <b>#arcade-server-uploads</b>, create a thread (if there isn't already one) and make your comment there.\n" +
                "You can click this element to open the Discord link.", "https://discord.gg/nB27X2JZcY"),
                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("When updating a level, make sure you include a message in the changelogs to let users know what changed in your level. Feel free to format it with dot points, or as a regular sentence / paragraph.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("If you have any issues with these guidelines or have a suggestion, please contact the BetterLegacy developer (RTMecha) and discuss it with him civilly.", EditorDocument.Element.Type.Text),
            });

            GenerateDocument("[PATCHED] Beatmap Objects", "The very objects that make up Project Arrhythmia levels.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("<b>Beatmap Objects</b> are the objects people use to create a variety of things for their levels. " +
                        "Whether it be backgrounds, characters, attacks, you name it! Below is a list of data Beatmap Objects have.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>ID [PATCHED]</b>\nThe ID is used for specifying a Beatmap Object, otherwise it'd most likely get lost in a sea of other objects! " +
                        "It's mostly used with parenting. This is patched because in unmodded PA, creators aren't able to see the ID of an object unless they look at the level.lsb.\n" +
                        "Clicking on the ID will copy it to your clipboard.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>LDM (Low Detail Mode) [MODDED]</b>\nLDM is useful for having objects not render for lower end devices. If the option is on and the user has " +
                        "Low Detail Mode enabled through the RTFunctions mod config, the Beatmap Object will not render.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_id_ldm.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Name [VANILLA]</b>\nNaming an object is incredibly helpful for readablility and knowing what an object does at a glance. " +
                        "Clicking your scroll wheel over it will flip any left / right.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_name_type.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Tags [MODDED]</b>\nBeing able to group objects together or even specify things about an object is possible with Object Tags. This feature " +
                        "is mostly used by modifiers, but can be used in other ways such as a \"DontRotate\" tag which prevents Player Shapes from rotating automatically.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_tags.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Locked [PATCHED]</b>\nIf on, prevents Beatmap Objects' start time from being changed. It's patched because unmodded PA doesn't " +
                        "have the toggle UI for this, however you can still use it in unmodded PA via hitting Ctrl + L.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Start Time [VANILLA]</b>\nUsed for when the Beatmap Object spawns.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_start_time.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Time of Death [VANILLA]</b>\nUsed for when the Beatmap Object despawns." +
                        "\n<b>[PATCHED]</b> No Autokill - Beatmap Objects never despawn. This option is viable in modded PA due to heavily optimized object code, so don't worry " +
                        "about having a couple of objects with this. Just make sure to only use this when necessary, like for backgrounds or a persistent character." +
                        "\n<b>[VANILLA]</b> Last KF - Beatmap Objects despawn once all animations are finished. This does NOT include parent animations. When the level " +
                        "time reaches after the last keyframe, the object despawns." +
                        "\n<b>[VANILLA]</b> Last KF Offset - Same as above but at an offset." +
                        "\n<b>[VANILLA]</b> Fixed Time - Beatmap Objects despawn at a fixed time, regardless of animations. Fixed time is Beatmap Objects Start Time with an offset added to it." +
                        "\n<b>[VANILLA]</b> Song Time - Same as above, except it ignores the Beatmap Object Start Time, despawning the object at song time.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Collapse [VANILLA]</b>\nBeatmap Objects in the editor timeline have their length shortened to the smallest amount if this is on.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_tod.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Parent Search [PATCHED]</b>\nHere you can search for an object to parent the Beatmap Object to. It includes Camera Parenting.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_parent_search.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Camera Parent [MODDED]</b>\nBeatmap Objects parented to the camera will always follow it, depending on the parent settings. This includes " +
                        "anything that makes the camera follow the player. This feature does exist in modern PA, but doesn't work the same way this does.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Clear Parent [MODDED]</b>\nClicking this will remove the Beatmap Object from its parent.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Parent Picker [ALPHA]</b>\nClicking this will activate a dropper. Right clicking will deactivate the dropper. Clicking on an object " +
                        "in the timeline will set the current selected Beatmap Objects parent to the selected Timeline Object.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Parent Display [VANILLA]</b>\nShows what the Beatmap Object is parented to. Clicking this button selects the parent. " +
                        "Hovering your mouse over it shows parent chain info in the Hover Info box.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_parent.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Parent Settings [PATCHED]</b>\nParent settings can be adjusted here. Each of the below settings refer to both " +
                        "position / scale / rotation. Position, scale and rotation are the rows and the types of Parent Settings are the columns.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Parent Type [VANILLA]</b>\nWhether the Beatmap Object applies this type of animation from the parent. " +
                        "It is the first column in the Parent Settings UI.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Parent Offset [VANILLA]</b>\nParent animations applied to the Beatmap Objects own parent chain get delayed at this offset. Normally, only " +
                        "the objects current parent gets delayed. It is the second column in the Parent Settings UI.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Parent Additive [MODDED]</b>\nForces Parent Offset to apply to every parent chain connected to the Beatmap Object. With this off, it only " +
                        "uses the Beatmap Objects' current parent. For example, say we have objects A, B, C and D. With this on, D delays the animation of every parent. With this off, it delays only C. " +
                        "It is the third column in the Parent Settings UI.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Parent Parallax [MODDED]</b>\nParent animations are multiplied by this amount, allowing for a parallax effect. Say the amount was 2 and the parent " +
                        "moves to position X 20, the object would move to 40 due to it being multiplied by 2. " +
                        "It is the fourth column in the Parent Settings UI.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_parent_more.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Origin [PATCHED]</b>\nOrigin is the offset applied to the visual of the Beatmap Object. Only usable for non-Empty object types. " +
                        "It's patched because of the number input fields instead of the direction buttons.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_origin.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Shape [PATCHED]</b>\nShape is whatever the visual of the Beatmap Object displays as. This doesn't just include actual shapes but stuff " +
                        "like text, images and player models too. More shape types and options were added. Unmodded PA does not include Image Shape, Pentagon Shape, Misc Shape, Player Shape.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_shape.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Render Depth [PATCHED]</b>\nDepth is how deep an object is in visual layers. Higher amount of Render Depth means the object is lower " +
                        "in the layers. Unmodded PA Legacy allows from 219 to -98. PA Alpha only allows from 40 to 0. Player is located at -60 depth. Z Axis Position keyframes use depth as a " +
                        "multiplied offset.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_depth.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Render Type [MODDED]</b>\nRender Type is if the visual of the Beatmap Object renders in the 2D layer or the 3D layer, aka Foreground / Background.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_render_type.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Layer [PATCHED]</b>\nLayer is what editor layer the Beatmap Object renders on. It can go as high as 2147483646. " +
                        "In unmodded PA its limited from layers 1 to 5, though in PA Editor Alpha another layer was introduced.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Bin [VANILLA]</b>\nBin is what row of the timeline the Beatmap Objects' timeline object renders on.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_editordata.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Object Debug [MODDED]</b>\nThis UI element only generates if UnityExplorer is installed. If it is, clicking on either button will inspect " +
                        "the internal data of the respective item.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_object_debug.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Integer Variable [MODDED]</b>\nEvery object has a whole number stored that Modifiers can use.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Modifiers [MODDED]</b>\nModifiers are made up of two different types: Triggers and Actions. " +
                            "Triggers check if a specified thing is happening and Actions do things depending on if any triggers are active or there aren't any. A detailed description of every modifier " +
                            "can be found in the Modifiers documentation. [WIP]", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_object_modifiers_edit.png", EditorDocument.Element.Type.Image),
            });

            GenerateDocument("[PATCHED] Beatmap Object Keyframes (WIP)", "The things that animate objects in different ways.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("The keyframes in the Beatmap Objects' keyframe timeline allow animating several aspects of a Beatmap Objects' visual.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>POSITION [PATCHED]</b>", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pos_none.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pos_normal.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pos_toggle.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pos_scale.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pos_static_homing.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pos_dynamic_homing.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>SCALE [VANILLA]</b>", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_sca_none.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_sca_normal.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_sca_toggle.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_sca_scale.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>ROTATION [VANILLA]</b>", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_rot_none.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_rot_normal.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_rot_toggle.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_rot_static_homing.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_rot_dynamic_homing.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>COLOR [PATCHED]</b>", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_col_none.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_col_dynamic_homing.png", EditorDocument.Element.Type.Image),
            });

            GenerateDocument("[PATCHED] Prefabs", "A package of objects that can be transfered from level to level. They can also be added to the level as a Prefab Object.", new List<EditorDocument.Element>
            {
                    new EditorDocument.Element("Prefabs are collections of objects grouped together for easy transfering from level to level.", EditorDocument.Element.Type.Text),
                    new EditorDocument.Element("<b>Name [VANILLA]</b>\nThe name of the Prefab. External prefabs gets saved with this as its file name, but all lowercase and " +
                        "spaces replaced with underscores.", EditorDocument.Element.Type.Text),
                    new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pc_name.png", EditorDocument.Element.Type.Image),
                    new EditorDocument.Element("<b>Offset [VANILLA]</b>\nThe delay set to every Prefab Objects' spawned objects related to this Prefab.", EditorDocument.Element.Type.Text),
                    new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pc_offset.png", EditorDocument.Element.Type.Image),
                    new EditorDocument.Element("<b>Type [PATCHED]</b>\nThe group name and color of the Prefab. Good for color coding what a Prefab does at a glance.", EditorDocument.Element.Type.Text),
                    new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pc_type.png", EditorDocument.Element.Type.Image),
                    new EditorDocument.Element("<b>Description [MODDED]</b>\nA good way to tell you and others what the Prefab does or contains in great detail.", EditorDocument.Element.Type.Text),
                    new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pc_description.png", EditorDocument.Element.Type.Image),
                    new EditorDocument.Element("<b>Seletion List [PATCHED]</b>\nShows every object, you can toggle the selection on any of them to add them to the prefab. All selected " +
                        "objects will be copied into the Prefab. This is patched because the UI and the code for it already existed in Legacy, it was just unused.", EditorDocument.Element.Type.Text),
                    new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pc_search.png", EditorDocument.Element.Type.Image),
                    new EditorDocument.Element("<b>Create [MODDED]</b>\nApplies all data and copies all selected objects to a new Prefab.", EditorDocument.Element.Type.Text),
                    new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_pc_create.png", EditorDocument.Element.Type.Image),

            });

            GenerateDocument("[PATCHED] Prefab Objects (OUTDATED)", "Individual instances of prefabs that spawn the packed objects at specified offsets.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("Prefab Objects are a copied version of the original prefab, placed into the level. They take all the objects stored in the original prefab " +
                    "and add them to the level, meaning you can have multiple copies of the same group of objects. Editing the objects of the prefab by expanding it applies all changes to " +
                    "the prefab, updating every Prefab Object (once collapsed back into a Prefab Object).", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Expand [VANILLA]</b>\nExpands all the objects contained within the original prefab into the level and deletes the Prefab Object.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_expand.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Layer [PATCHED]</b>\nWhat Editor Layer the Prefab Object displays on. Can go from 1 to 2147483646. In unmodded Legacy its 1 to 5.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_layer.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Time of Death [MODDED]</b>\nTime of Death allows every object spawned from the Prefab Object still alive at a certain point to despawn." +
                    "\nRegular - Just how the game handles Prefab Objects kill time normally." +
                    "\nStart Offset - Kill time is offset plus the Prefab Object start time." +
                    "\nSong Time - Kill time is song time, so no matter where you change the start time to the kill time remains the same.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_tod.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Locked [PATCHED]</b>\nIf on, prevents Prefab Objects' start time from being changed. It's patched because unmodded PA doesn't " +
                    "have the toggle UI for this, however you can still use it in unmodded PA via hitting Ctrl + L.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Collapse [PATCHED]</b>\nIf on, collapses the Prefab Objects' timeline object. This is patched because it literally doesn't " +
                    "work in unmodded PA.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Start Time [VANILLA]</b>\nWhere the objects spawned from the Prefab Object start.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_time.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Position Offset [PATCHED]</b>\nEvery objects' top-most-parent has its position set to this offset. Unmodded PA technically has this " +
                    "feature, but it's not editable in the editor.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_pos_offset.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Scale Offset [PATCHED]</b>\nEvery objects' top-most-parent has its scale set to this offset. Unmodded PA technically has this " +
                    "feature, but it's not editable in the editor.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_sca_offset.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Rotation Offset [PATCHED]</b>\nEvery objects' top-most-parent has its rotation set to this offset. Unmodded PA technically has this " +
                    "feature, but it's not editable in the editor.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_rot_offset.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Repeat [MODDED]</b>\nWhen spawning the objects from the Prefab Object, every object gets repeated a set amount of times" +
                    "with their start offset added onto each time they repeat depending on the Repeat Offset Time set. The data for Repeat Count and Repeat Offset Time " +
                    "already existed in unmodded PA, it just went completely unused.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_repeat.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Speed [MODDED]</b>\nHow fast each object spawned from the Prefab Object spawns and is animated.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_object_speed.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Lead Time / Offset [VANILLA]</b>\nEvery Prefab Object starts at an added offset from the Offset amount. I have no idea why " +
                    "it's called Lead Time here even though its Offset everywhere else.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_lead.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Name [MODDED]</b>\nChanges the name of the original Prefab related to the Prefab Object. This is modded because you couldn't " +
                    "change this in the Prefab Object editor.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_name.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Type [MODDED]</b>\nChanges the Type of the original Prefab related to the Prefab Object. This is modded because you couldn't " +
                    "change this in the Prefab Object editor. (You can scroll-wheel over the input field to change the type easily)", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_type.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Save [MODDED]</b>\nSaves all changes made to the original Prefab to any External Prefab with a matching name.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_save.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Count [MODDED]</b>\nTells how many objects are in the original Prefab and how many Prefab Objects there are in the timeline " +
                    "for the Prefab. The Prefab Object Count goes unused for now...?", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_prefab_counts.png", EditorDocument.Element.Type.Image),
            });

            GenerateDocument("[LEGACY] Background Objects (WIP)", "The classic 3D style backgrounds.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("Background Object intro.", EditorDocument.Element.Type.Text),
            });

            GenerateDocument("[PATCHED] Events", "Effects to make your level pretty or to animate properties of the level.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("BetterLegacy has a total of 40 event types. Below is a list of them all and what they do:", EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>[VANILLA]</b> Move - Moves the camera left, right, up and down.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[VANILLA]</b> Position X\n" +
                                    "<b>[VANILLA]</b> Position Y", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> Zoom - Zooms the camera in and out.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[VANILLA]</b> Zoom", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> Rotate - Rotates the camera around.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[VANILLA]</b> Rotation", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[PATCHED]</b> Shake - Shakes the camera.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[VANILLA]</b> Shake Intensity\n" +
                                    "<b>[MODDED]</b> Direction X\n" +
                                    "<b>[MODDED]</b> Direction Y\n" +
                                    "<b>[MODDED]</b> Smoothness\n" +
                                    "<b>[MODDED]</b> Speed", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[PATCHED]</b> Theme - The current set of colors to use for a level.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[VANILLA]</b> Theme selection", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> Chroma - Stretches the colors from the border of the screen.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[VANILLA]</b> Chromatic Aberation Amount", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[PATCHED]</b> Bloom - Applies a glowy effect on the screen.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[VANILLA]</b> Bloom Amount\n" +
                                    "<b>[ALPHA]</b> Diffusion\n" +
                                    "<b>[MODDED]</b> Threshold\n" +
                                    "<b>[MODDED]</b> Anamorphic Ratio\n" +
                                    "<b>[ALPHA]</b> Color\n" +
                                    "<b>[MODDED]</b> HSV (Hue / Saturation / Value)", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[PATCHED]</b> Vignette - Applies a dark / light border around the screen.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[VANILLA]</b> Intensity\n" +
                                    "<b>[VANILLA]</b> Smoothness\n" +
                                    "<b>[LEGACY]</b> Rounded\n" +
                                    "<b>[VANILLA]</b> Roundness\n" +
                                    "<b>[VANILLA]</b> Center Position\n" +
                                    "<b>[ALPHA]</b> Color\n" +
                                    "<b>[MODDED]</b> HSV (Hue / Saturation / Value)", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[PATCHED]</b> Lens - Pushes the center of the screen in and out.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[VANILLA]</b> Lens Distort Amount\n" +
                                    "<b>[ALPHA]</b> Center X\n" +
                                    "<b>[ALPHA]</b> Center Y\n" +
                                    "<b>[MODDED]</b> Intensity X\n" +
                                    "<b>[MODDED]</b> Intensity Y\n" +
                                    "<b>[MODDED]</b> Scale", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> Grain - Adds a static effect to the screen (or makes it flash in Legacy if Grain size is 0.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[VANILLA]</b> Intensity Amount\n" +
                                    "<b>[VANILLA]</b> Size of Grains\n" +
                                    "<b>[VANILLA]</b> Colored", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[MODDED]</b> ColorGrading - Affects the colors of the whole level, regardless of theme. If the level is converted to the VG format, this event will be turned into the Hue event and will only have the Hueshift value.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[ALPHA]</b> Hueshift\n" +
                                    "<b>[MODDED]</b> Contrast\n" +
                                    "<b>[MODDED]</b> Saturation\n" +
                                    "<b>[MODDED]</b> Temperature\n" +
                                    "<b>[MODDED]</b> Tint\n" +
                                    "<b>[MODDED]</b> Gamma X\n" +
                                    "<b>[MODDED]</b> Gamma Y\n" +
                                    "<b>[MODDED]</b> Gamma Z\n" +
                                    "<b>[MODDED]</b> Gamma W", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[ALPHA]</b> Gradient - Applies a gradient over the screen. If the level is converted to the VG format, all values (except OHSV) will be carried over.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[ALPHA]</b> Intensity\n" +
                                    "<b>[ALPHA]</b> Rotation\n" +
                                    "<b>[ALPHA]</b> Color Top\n" +
                                    "<b>[MODDED]</b> OHSV Top (Opacity / Hue / Saturation / Value)\n" +
                                    "<b>[ALPHA]</b> Color Bottom\n" +
                                    "<b>[MODDED]</b> OHSV Bottom (Opacity / Hue / Saturation / Value)\n" +
                                    "<b>[ALPHA]</b> Mode", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[ALPHA]</b> Player Force - Forces the player to constantly move in a direction. If the level is converted to the VG format, all values will be carried over to the Player event.\n" +
                                    "<b>Values</b>:\n" +
                                    "<b>[ALPHA]</b> Force X\n" +
                                    "<b>[ALPHA]</b> Force Y", EditorDocument.Element.Type.Text),
                // todo: add the rest
            });

            GenerateDocument("[PATCHED] Text Objects", "Flavor your levels with text!", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("Text Objects can be used in extensive ways, from conveying character dialogue to decoration. This document is for showcasing usable " +
                    "fonts and formats Text Objects can use.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>- FORMATTING -</b>", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> <noparse><b></noparse> - For making text <b>BOLD</b>. Use <noparse></b></noparse> to clear.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> <noparse><i></noparse> - For making text <i>italic</i>. Use <noparse></i></noparse> to clear.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> <noparse><u></noparse> - Gives text an <u>underline</u>. Use <noparse></u></noparse> to clear.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> <noparse><s></noparse> - Gives text a <s>strikethrough</s>. Use <noparse></s></noparse> to clear.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> <br <pos=136>> - Line break.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> <noparse><material=LiberationSans SDF - Outline></noparse> - Outline effect that only works on the <b><i>LiberationSans SDF</b></i> font.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>[VANILLA]</b> <noparse><line-height=25></noparse> - Type any number you want in place of the 25. Use <noparse></line-height></noparse> to clear.", EditorDocument.Element.Type.Text)
                {
                    Function = () =>
                    {
                        EditorManager.inst.DisplayNotification($"Copied material!", 2f, EditorManager.NotificationType.Success);
                        LSText.CopyToClipboard($"<material=LiberationSans SDF - Outline>");
                    }
                },

                new EditorDocument.Element("<b><size=36><align=center>- FONTS -</b>", EditorDocument.Element.Type.Text, 40f),
                new EditorDocument.Element(RTFile.BepInExAssetsPath + "Documentation/doc_fonts.png", EditorDocument.Element.Type.Image)
                { Function = () => { RTFile.OpenInFileBrowser.OpenFile(RTFile.ApplicationDirectory + RTFile.BepInExAssetsPath + "Documentation/doc_fonts.png"); }},
                new EditorDocument.Element("To use a font, do <font=Font Name>. To clear, do <noparse></font></noparse>. Click on one of the fonts below to copy the <font=Font Name> to your clipboard. " +
                    "Click on the image above to open the folder to the documentation assets folder where a higher resolution screenshot is located.", EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b><size=30><align=center>- PROJECT ARRHYTHMIA FONTS -</b>", EditorDocument.Element.Type.Text, 32f),
                FontElement(EditorDocument.SupportType.MODDED, "Arrhythmia", "The font from the earliest builds of Project Arrhythmia."),
                FontElement(EditorDocument.SupportType.MODDED, "Fredoka One", "The font used in the Vitamin Games website."),
                FontElement(EditorDocument.SupportType.MODDED, "Inconsolata Variable", "The default PA font."),
                FontElement(EditorDocument.SupportType.MODDED, "LiberationSans SDF", "An extra font Vanilla Legacy has."),
                FontElement(EditorDocument.SupportType.MODDED, "File Deletion", "A font pretty similar to the modern PA title font."),

                new EditorDocument.Element("<b><size=30><align=center>- GEOMETRY DASH FONTS -</b>", EditorDocument.Element.Type.Text, 32f),
                FontElement(EditorDocument.SupportType.MODDED, "Oxygene", "The font used for the title of Geometry Dash."),
                FontElement(EditorDocument.SupportType.MODDED, "Pusab", "The main font used in the hit game Geometry Dash. And yes, it is the right one."),
                FontElement(EditorDocument.SupportType.MODDED, "Minecraftory", "Geometry Dash font mainly used in Geometry Dash SubZero."),

                new EditorDocument.Element("<b><size=30><align=center>- COMIC FONTS -</b>", EditorDocument.Element.Type.Text, 32f),
                FontElement(EditorDocument.SupportType.MODDED, "Adam Warren Pro Bold", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Adam Warren Pro BoldItalic", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Adam Warren Pro", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "BadaBoom BB", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Komika Hand", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Komika Hand Bold", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Komika Slim", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Komika Hand BoldItalic", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Komika Hand Italic", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Komika Jam", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Komika Jam Italic", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Komika Slick Italic", "A comic style font."),
                FontElement(EditorDocument.SupportType.MODDED, "Komika Slim Italic", "A comic style font."),

                new EditorDocument.Element("<b><size=30><align=center>- BIONICLE FONTS -</b>", EditorDocument.Element.Type.Text, 32f),
                FontElement(EditorDocument.SupportType.MODDED, "Matoran Language 1", "The language used by the Matoran in the BIONICLE series."),
                FontElement(EditorDocument.SupportType.MODDED, "Matoran Language 2", "The language used by the Matoran in the BIONICLE series."),
                FontElement(EditorDocument.SupportType.MODDED, "Piraka Theory", "The language used by the Piraka in the BIONICLE series."),
                FontElement(EditorDocument.SupportType.MODDED, "Piraka", "The language used by the Piraka in the BIONICLE series."),
                FontElement(EditorDocument.SupportType.MODDED, "Rahkshi", "The font used for promoting the Rahkshi sets in the BIONICLE series."),

                new EditorDocument.Element("<b><size=30><align=center>- UNDERTALE / DELTARUNE FONTS -</b>", EditorDocument.Element.Type.Text, 32f),
                FontElement(EditorDocument.SupportType.MODDED, "Monster Friend Back", "A font based on UNDERTALE's title."),
                FontElement(EditorDocument.SupportType.MODDED, "Monster Friend Fore", "A font based on UNDERTALE's title."),
                FontElement(EditorDocument.SupportType.MODDED, "Determination Mono", "The font UNDERTALE/deltarune uses for its interfaces."),
                FontElement(EditorDocument.SupportType.MODDED, "determination sans", "sans undertale."),
                FontElement(EditorDocument.SupportType.MODDED, "Determination Wingdings", "Beware the man who speaks in hands."),
                FontElement(EditorDocument.SupportType.MODDED, "Hachicro", "The font used by UNDERTALE's hit text."),

                new EditorDocument.Element("<b><size=30><align=center>- TRANSFORMERS FONTS -</b>", EditorDocument.Element.Type.Text, 32f),
                FontElement(EditorDocument.SupportType.MODDED, "Ancient Autobot", "The language used by ancient Autobots in the original Transformers cartoon."),
                FontElement(EditorDocument.SupportType.MODDED, "Revue", "The font used early 2000s Transformers titles."),
                FontElement(EditorDocument.SupportType.MODDED, "Revue 1", "The font used early 2000s Transformers titles."),
                FontElement(EditorDocument.SupportType.MODDED, "Transdings", "A font that contains a ton of Transformer insignias / logos. Below is an image featuring each letter of the alphabet."),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_tf.png", EditorDocument.Element.Type.Image),
                FontElement(EditorDocument.SupportType.MODDED, "Transformers Movie", "A font based on the Transformers movies title font."),

                new EditorDocument.Element("<b><size=30><align=center>- LANGUAGE SUPPORT FONTS -</b>", EditorDocument.Element.Type.Text, 32f),
                FontElement(EditorDocument.SupportType.MODDED, "Monomaniac One", "Japanese support font."),
                FontElement(EditorDocument.SupportType.MODDED, "Roboto Mono Bold", "Russian support font."),
                FontElement(EditorDocument.SupportType.MODDED, "Roboto Mono Italic", "Russian support font."),
                FontElement(EditorDocument.SupportType.MODDED, "Roboto Mono Light 1", "Russian support font."),
                FontElement(EditorDocument.SupportType.MODDED, "Roboto Mono Light Italic", "Russian support font."),
                FontElement(EditorDocument.SupportType.MODDED, "Roboto Mono Light Italic 1", "Russian support font."),
                FontElement(EditorDocument.SupportType.MODDED, "Roboto Mono Thin", "Russian support font."),
                FontElement(EditorDocument.SupportType.MODDED, "Roboto Mono Thin 1", "Russian support font."),
                FontElement(EditorDocument.SupportType.MODDED, "Roboto Mono Thin Italic", "Russian support font."),
                FontElement(EditorDocument.SupportType.MODDED, "Roboto Mono Thin Italic 1", "Russian support font."),
                FontElement(EditorDocument.SupportType.MODDED, "Pixellet", "A neat pixel font that supports Thai."),
                FontElement(EditorDocument.SupportType.MODDED, "Angsana Bold", "A font suggested by KarasuTori. Supports non-English languages like Thai."),
                FontElement(EditorDocument.SupportType.MODDED, "Angsana Italic", "A font suggested by KarasuTori. Supports non-English languages like Thai."),
                FontElement(EditorDocument.SupportType.MODDED, "Angsana Bold Italic", "A font suggested by KarasuTori. Supports non-English languages like Thai."),
                FontElement(EditorDocument.SupportType.MODDED, "VAG Rounded", "A font suggested by KarasuTori. Supports non-English languages like Russian."),

                new EditorDocument.Element("<b><size=30><align=center>- OTHER FONTS -</b>", EditorDocument.Element.Type.Text, 32f),
                FontElement(EditorDocument.SupportType.MODDED, "About Friend", "A font suggested by Ama."),
                FontElement(EditorDocument.SupportType.MODDED, "Flow Circular", "A fun line font suggested by ManIsLiS."),
                FontElement(EditorDocument.SupportType.MODDED, "Minecraft Text Bold", "The font used for the text UI in Minecraft."),
                FontElement(EditorDocument.SupportType.MODDED, "Minecraft Text BoldItalic", "The font used for the text UI in Minecraft."),
                FontElement(EditorDocument.SupportType.MODDED, "Minecraft Text", "The font used for the text UI in Minecraft."),
                FontElement(EditorDocument.SupportType.MODDED, "Nexa Book", "A font suggested by CubeCube."),
                FontElement(EditorDocument.SupportType.MODDED, "Nexa Bold", "A font suggested by CubeCube."),
                FontElement(EditorDocument.SupportType.MODDED, "Comic Sans", "You know the font."),
                FontElement(EditorDocument.SupportType.MODDED, "Comic Sans Bold", "You know the font."),
                FontElement(EditorDocument.SupportType.MODDED, "Comic Sans Hairline", "You know the font."),
                FontElement(EditorDocument.SupportType.MODDED, "Comic Sans Light", "You know the font."),
                FontElement(EditorDocument.SupportType.MODDED, "Sans Sans", "Sans Sans."),
                FontElement(EditorDocument.SupportType.MODDED, "Angsana Z", "Classical font."),
            });

            GenerateDocument("[MODDED] Player Editor (WIP)", "Time to get creative with creative players!", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("In BetterLegacy, you can create a fully customized Player Model.", EditorDocument.Element.Type.Text),
            });

            GenerateDocument("[PATCHED] Markers (OUTDATED)", "Organize and remember details about a level.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("Markers can organize certain parts of your level or help with aligning objects to a specific time.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("In the image below is two types of markers. The blue marker is the Audio Marker and the marker with a circle on the top is just a Marker. " +
                    "Left clicking on the Marker's circle knob moves the Audio Marker to the regular Marker. Right clicking the Marker's circle knob deletes it.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_marker_timeline.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Name [VANILLA]</b>\nThe name of the Marker. This renders next to the Marker's circle knob in the timeline.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_marker_name.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Time [VANILLA]</b>\nThe time the Marker renders at in the timeline.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_marker_time.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Description [PATCHED]</b>\nDescription helps you remember details about specific parts of a song or even stuff about the level you're " +
                    "editing. Typing setLayer(1) will set the editor layer to 1 when the Marker is selected. You can also have it be setLayer(events), setLayer(objects), setLayer(toggle), which " +
                    "sets the layer type to those respective types (toggle switches between Events and Objects layer types). Fun fact, the title for description in the UI in unmodded Legacy " +
                    "said \"Name\" lol.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_marker_description.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Colors [PATCHED]</b>\nWhat color the marker displays as. You can customize the colors in the Settings window.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_marker_colors.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Index [MODDED]</b>\nThe number of the Marker in the list.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_marker_index.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("On the right-hand-side of the Marker Editor window is a list of markers. At the top is a Search field and a Delete Markers button. " +
                    "Delete Markers clears every marker in the level and closes the Marker Editor.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_marker_delete.png", EditorDocument.Element.Type.Image),
            });

            GenerateDocument("[PATCHED] Title Bar (OUTDATED)", "The thing at the top of the editor UI with dropdowns.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("Title Bar has the main functions for loading, saving and editing.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_td.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>File [PATCHED]</b>" +
                    "\nPowerful functions related to the application or files." +
                    "\n<b>[VANILLA]</b> New Level - Creates a new level." +
                    "\n<b>[VANILLA]</b> Open Level - Opens the level list popup, where you can search and select a level to load." +
                    "\n<b>[VANILLA]</b> Open Level Folder - Opens the current loaded level's folder in your local file explorer." +
                    "\n<b>[MODDED]</b> Open Level Browser - Opens a built-in browser to open a level from anywhere on your computer." +
                    "\n<b>[MODDED]</b> Level Combiner - Combines multiple levels together." +
                    "\n<b>[VANILLA]</b> Save - Saves the current level." +
                    "\n<b>[PATCHED]</b> Save As - Saves a copy of the current level." +
                    "\n<b>[VANILLA]</b> Toggle Play Mode - Opens preview mode." +
                    "\n<b>[MODDED]</b> Switch to Arcade Mode - Switches to the handling of level loading in Arcade." +
                    "\n<b>[MODDED]</b> Quit to Arcade - Opens the Input Select scene just before loading arcade levels." +
                    "\n<b>[VANILLA]</b> Quit to Main Menu - Exits to the main menu." +
                    "\n<b>[VANILLA]</b> Quit Game - Quits the game.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_td_file.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Edit [PATCHED]</b>" +
                    "\nHow far can you edit in a modded editor?" +
                    "\n<b>[PATCHED]</b> Undo - Undoes the most recent action. Still heavily WIP. (sorry)" +
                    "\n<b>[PATCHED]</b> Redo - Same as above but goes back to the recent action when undone." +
                    "\n<b>[MODDED]</b> Search Objects - Search for specific objects by name or index. Hold Left Control to take yourself to the object in the timeline." +
                    "\n<b>[MODDED]</b> Preferences - Modify editor specific mod configs directly in the editor. Also known as Editor Properties." +
                    "\n<b>[MODDED]</b> Player Editor - Only shows if you have CreativePlayers installed. Opens the Player Editor." +
                    "\n<b>[MODDED]</b> View Keybinds - Customize the keybinds of the editor in any way you want.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_td_edit.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>View [PATCHED]</b>" +
                    "\nView specific things." +
                    "\n<b>[MODDED]</b> Get Example - Only shows if you have ExampleCompanion installed. It summons Example to the scene." +
                    "\n<b>[VANILLA]</b> Show Help - Toggles the Info box.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_td_view.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Steam [VANILLA]</b>" +
                    "\nView Steam related things... even though modded PA doesn't use Steam anymore lol" +
                    "\n<b>[VANILLA]</b> Open Workshop - Opens a link to the Steam workshop." +
                    "\n<b>[VANILLA]</b> Publish / Update Level - Opens the Metadata Editor / Level Uploader.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_td_steam.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Help [PATCHED]</b>" +
                    "\nGet some help." +
                    "\n<b>[MODDED]</b> Modder's Discord - Opens a link to the mod creator's Discord server." +
                    "\n<b>[MODDED]</b> Watch PA History - Since there are no <i>modded</i> guides yet, this just takes you to the System Error BTS PA History playlist." +
                    "\n<b>[MODDED]</b> Wiki / Documentation - In-editor documentation of everything the game has to offer. You're reading it right now!", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_td_help.png", EditorDocument.Element.Type.Image),
            });

            GenerateDocument("[PATCHED] Timeline Bar (OUTDATED)", "The main toolbar used for editing main editor things such as audio time, editor layers, etc.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("The Timeline Bar is where you can see and edit general game and editor info.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Audio Time (Precise) [MODDED]</b>\nText shows the precise audio time. This can be edited to set a specific time for the audio.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Audio Time (Formatted) [VANILLA]</b>\nText shows the audio time formatted like \"minutes.seconds.milliseconds\". Clicking this sets the " +
                    "audio time to 0.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Pause / Play [VANILLA]</b>\nPressing this toggles if the song is playing or not.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Pitch [PATCHED]</b>\nThe speed of the song. Clicking the buttons adjust the pitch by 0.1, depending on the direction the button is facing.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_tb_audio.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Editor Layer [PATCHED]</b>\nEditor Layer is what objects show in the timeline, depending on their own Editor Layer. " +
                    "It can go as high as 2147483646. In unmodded PA its limited from layers 1 to 5, though in PA Editor Alpha another layer was introduced.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Editor Layer Type [MODDED]</b>\nWhether the timeline shows objects or event keyframes / checkpoints.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_tb_layer.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Prefab [VANILLA]</b>\nOpens the Prefab list popups (Internal & External).", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Object [PATCHED]</b>\nOpens a popup featuring different object templates such as Decoration, Empty, etc. It's patched because " +
                    "Persistent was replaced with No Autokill.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Marker [VANILLA]</b>\nCreates a Marker.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>BG [VANILLA]</b>\nOpens a popup to open the BG editor or create a new BG.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_tb_create.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Preview Mode [VANILLA]</b>\nSwitches the game to Preview Mode.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_tb_preview_mode.png", EditorDocument.Element.Type.Image),
            });

            GenerateDocument("[MODDED] Keybinds (WIP)", "Perform specific actions when pressing set keys.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("Keybinds intro.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_keybind_list.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_keybind_editor.png", EditorDocument.Element.Type.Image),
            });

            GenerateDocument("[MODDED] Object Modifiers", "Make your levels dynamic!", new List<EditorDocument.Element>
            {
                //new EditorDocument.Element("BetterLegacy adds a trigger / action based system to Beatmap Objects called \"Modifiers\". " +
                //    "Modifiers have two types: Triggers check if something is happening and if it is, it activates any Action type modifiers. If there are no Triggers, then the Action modifiers " +
                //    "activates. This document is heavily WIP and will be added to over time.", EditorDocument.Element.Type.Text),
                //new EditorDocument.Element("<b>setPitch</b> - Modifies the speed of the game and the pitch of the audio. It sets a multiplied offset from the " +
                //    "audio keyframe's pitch value. However unlike the event keyframe, setPitch can go into the negatives allowing for reversed audio.", EditorDocument.Element.Type.Text),
                //new EditorDocument.Element("<b>addPitch</b> - Does the same as above, except adds to the pitch offset.", EditorDocument.Element.Type.Text),
                //new EditorDocument.Element("<b>setMusicTime</b> - Sets the Audio Time to go to any point in the song, allowing for skipping specific sections of a song.", EditorDocument.Element.Type.Text),
                //new EditorDocument.Element("<b>playSound</b> - Plays an external sound. The following details what each value in the modifier does." +
                //    "\nPath - If global is on, path should be set to something within beatmaps/soundlibrary directory. If global is off, then the path should be set to something within the level " +
                //    "folder that has level.lsb and metadata.lsb." +
                //    "\nGlobal - Affects the above setting in the way described." +
                //    "\nPitch - The speed of the sound played." +
                //    "\nVolume - How loud the sound is." +
                //    "\nLoop - If the sound should loop while the Modifier is active.", EditorDocument.Element.Type.Text),
                //new EditorDocument.Element("<b>playSoundOnline</b> - Same as above except plays from a link. The global toggle does nothing here.", EditorDocument.Element.Type.Text),
                //new EditorDocument.Element("<b>loadLevel</b> - Loads a level from the current level folder path.", EditorDocument.Element.Type.Text),
                //new EditorDocument.Element("<b>loadLevelInternal</b> - Same as above, except it always loads from the current levels own path.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Some objects in BetterLegacy have a list of \"modifiers\" that can be used to affect that object in a lot of different ways. This documentation only documents Beatmap Object Modifiers, but some other objects have them too.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>TRIGGERS</b>\n" +
                                        "These types of modifiers will check if something is happening and if it is, will allow other modifiers in the list to run.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>ACTIONS</b>\n" +
                                        "Action modifiers modify the level or object if it runs.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("If an object is active or \"Ignore Lifespan\" is turned on, the modifier loop will run.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("With \"Order Matters\" off, it requires all trigger modifiers to be active to run the action modifiers, regardless of order.\n" +
                                "With it on, it considers the order of the modifiers. It'll check triggers up until it hits an action and if all " +
                                "(or some, depends on the else if toggle) triggers are active, then it'll run the next set of action modifiers. Below is an example:", EditorDocument.Element.Type.Text),
                new EditorDocument.Element(
                                "- Action 1 (runs regardless of triggers since there are no triggers before it)\n\n" +
                                "- Trigger 1\n" +
                                "- Trigger 2\n" +
                                "- Action 2 (runs only if Triggers 1 and 2 are triggered)\n\n" +
                                "- Trigger 3\n" +
                                "- Action 3 (runs only if Trigger 3 is triggered. Doesn't care about Triggers 1 and 2)\n\n" +
                                "- Trigger 4\n" +
                                "- Trigger 5 (else if)\n" +
                                "- Action 4 (runs only if Trigger 4 or 5 are triggered. Doesn't care about triggers before Action 3)\n" +
                                "- Action 5 (same as Action 4, this just shows you can have multiple actions after a set of triggers)"
                                , EditorDocument.Element.Type.Text, 370f),
                new EditorDocument.Element("Modifiers can be told to only run once whenever it runs by turning \"Constant\" off. Some modifiers have specific behavior with it on / off.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Triggers can be turned into a \"Not Gate\" by turning \"Not\" on. This will require the triggers' check to be the opposite of what it would normally be.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Triggers also have an \"else if\" toggle that will optionally be checked if the previous triggers were not triggered.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Some modifiers support affecting a group of Beatmap Objects if they're being used for such. You can customize if the group should only be within the Prefab the object spawned from by turning \"Prefab Group Only\" on.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("You can organize modifiers in the editor by right clicking them and using the \"Move\" functions.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Below is a list of a few modifier groups and what they generally do. Does not include every modifier as there's far too many to count (300+).", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>AUDIO MODIFIERS</b>\n" +
                                        "These all have some relevance to the current audio, or its own audio.\n" +
                                        "- setPitch (Action)\n" +
                                        "- addPitch (Action)\n" +
                                        "- setMusicTime (Action)\n" +
                                        "- playSound (Action)\n" +
                                        "- audioSource (Action)\n" +
                                        "- pitchEquals (Trigger)\n" +
                                        "- musicTimeGreater (Trigger)\n" +
                                        "- musicTimeLesser (Trigger)\n" +
                                        "- musicPlaying (Trigger)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>LEVEL MODIFIERS</b>\n" +
                                        "Not to be confused with the Level Modifiers that can be converted to Alpha level triggers. These modifiers have a connection with the level or multiple in some way.\n" +
                                        "- loadLevelID (Action)\n" +
                                        "- endLevel (Action)\n" +
                                        "- inZenMode (Trigger)\n" +
                                        "- inNormal (Trigger)\n" +
                                        "- levelRankEquals (Trigger)\n" +
                                        "- levelCompleted (Trigger)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>COMPONENT MODIFIERS</b>\n" +
                                        "These act as a component that directly modifies how the object looks / works.\n" +
                                        "- blur (Action)\n" +
                                        "- blurColored (Action)\n" +
                                        "- doubleSided (Action)\n" +
                                        "- particleSystem (Action)\n" +
                                        "- trailRenderer (Action)\n" +
                                        "- rigidbody (Action) < Required for objectCollide triggers to work.\n" +
                                        "- gravity (Action)\n" +
                                        "- objectCollide (Trigger)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>PREFAB MODIFIERS</b>\n" +
                                        "These can spawn / despawn Prefabs directly into a level.\n" +
                                        "- spawnPrefab (Action)\n" +
                                        "- spawnPrefabOffset (Action)\n" +
                                        "- spawnMultiPrefab (Action)\n" +
                                        "- spawnMultiPrefabOffset (Action)\n" +
                                        "- clearSpawnedPrefabs (Action)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>PLAYER MODIFIERS</b>\n" +
                                        "Again, not to be confused with the Player Modifiers that Player Models use. These affect the Players.\n" +
                                        "- playerHeal (Action)\n" +
                                        "- playerHit (Action)\n" +
                                        "- playerKill (Action)\n" +
                                        "- playerBoost (Action)\n" +
                                        "- gameMode (Action)\n" +
                                        "- setPlayerModel (Action)\n" +
                                        "- blackHole (Action)\n" +
                                        "- playerCollide (Trigger)\n" +
                                        "- playerHealthEquals (Trigger)\n" +
                                        "- playerDeathsEquals (Trigger)\n" +
                                        "- playerBoosting (Trigger)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>VARIABLE MODIFIERS</b>\n" +
                                        "Stores and compares variables for use across the level.\n" +
                                        "- addVariable (Action)\n" +
                                        "- setVariable (Action)\n" +
                                        "- variableEquals (Trigger)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>ENABLE / DISABLE MODIFIERS</b>\n" +
                                        "Hides / unhides an object.\n" +
                                        "- enableObject (Action)\n" +
                                        "- enableObjectTree (Action)\n" +
                                        "- disableObject (Action)\n" +
                                        "- disableObjectTree (Action)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>REACTIVE MODIFIERS</b>\n" +
                                        "Makes an object react to the audio, like the Background Objects do already.\n" +
                                        "- reactivePosChain (Action)\n" +
                                        "- reactiveScaChain (Action)\n" +
                                        "- reactiveRotChain (Action)\n" +
                                        "- reactiveCol (Action)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>EVENT MODIFIERS</b>\n" +
                                        "Offsets Event Keyframes.\n" +
                                        "- eventOffset (Action)\n" +
                                        "- eventOffsetAnimate (Action)\n" +
                                        "- vignetteTracksPlayer (Action)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>COLOR MODIFIERS</b>\n" +
                                        "The color of the object.\n" +
                                        "- lerpColor (Action)\n" +
                                        "- setAlpha (Action)\n" +
                                        "- copyColor (Action)\n" +
                                        "- setColorHex (Action)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>TEXT MODIFIERS</b>\n" +
                                        "The text of a text object can be changed with these.\n" +
                                        "- setText (Action)\n" +
                                        "- formatText (Action)\n" +
                                        "- textSequence (Action)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>ANIMATION MODIFIERS</b>\n" +
                                        "Dynamically (or statically) animate objects in various ways.\n" +
                                        "- animateObject (Action)\n" +
                                        "- applyAnimation (Action)\n" +
                                        "- copyAxis (Action)\n" +
                                        "- legacyTail (Action)\n" +
                                        "- axisEquals (Trigger)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>SHAPE MODIFIERS</b>\n" +
                                        "Affects the shape of the object.\n" +
                                        "- translateShape (Action)\n" +
                                        "- backgroundShape (Action)\n" +
                                        "- sphereShape (Action)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>INPUT MODIFIERS</b>\n" +
                                        "Set of triggers related to keyboards, controllers and the mouse cursor.\n" +
                                        "- keyPressDown (Trigger)\n" +
                                        "- controlPressDown (Trigger)\n" +
                                        "- mouseButtonDown (Trigger)\n" +
                                        "- mouseOver (Trigger)\n"
                                        , EditorDocument.Element.Type.Text),

                new EditorDocument.Element("<b>MISC MODIFIERS</b>\n" +
                                        "These don't really fit into a category but are interesting enough to let you know they exist.\n" +
                                        "- setWindowTitle (Action)\n" +
                                        "- setDiscordStatus (Action)\n" +
                                        "- loadInterface (Action)\n" +
                                        "- disableModifier (Trigger)\n"
                                        , EditorDocument.Element.Type.Text),
            });

            GenerateDocument("[MODDED] Math Evaluation", "Some places you can write out a math equation and get a result from it.", new List<EditorDocument.Element>
            {
                LinkElement("Math Evaluation is implemented from ILMath, created by Reimnop. If you want to know more, visit the link: https://github.com/Reimnop/ILMath", "https://github.com/Reimnop/ILMath"),
                new EditorDocument.Element("Below is a list of variables that can be used with math evaluation.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>deathCount</b> - Amount of deaths in a level (Arcade only).\n" +
                                     "<b>hitCount</b> - Amount of hits in a level (Arcade only).\n" +
                                     "<b>actionMoveX</b> - WASD / joystick move.\n" +
                                     "<b>actionMoveY</b> - WASD / joystick move.\n" +
                                     "<b>time</b> - Unity's time property. (seconds since game start)\n" +
                                     "<b>deltaTime</b> - Unity's deltaTime property.\n" +
                                     "<b>audioTime</b> - The main audio time.\n" +
                                     "<b>smoothedTime</b> - Catalyst animation time.\n" +
                                     "<b>volume</b> - Current music volume.\n" +
                                     "<b>pitch</b> - Current music pitch (or game speed).\n" +
                                     "<b>forwardPitch</b> - Current music pitch (or game speed) but always above 0.001.\n" +
                                     "<b>camPosX</b> - Position X of the camera.\n" +
                                     "<b>camPosY</b> - Position Y of the camera.\n" +
                                     "<b>camZoom</b> - Zoom of the camera.\n" +
                                     "<b>camRot</b> - Rotation of the camera.\n" +
                                     "<b>player0PosX</b> - Position X of a specific player. You can swap the 0 out with a different number to get a different players' position X. If that number is higher than or equal to the player count, the result will be 0.\n" +
                                     "<b>player0PosY</b> - Position Y of a specific player. You can do the same as the above variable.\n" +
                                     "<b>player0Rot</b> - Rotation of a specific player. You can do the same as the above variable.\n" +
                                     "<b>player0Health</b> - Health of a specific player. You can do the same as the above variable.\n" +
                                     "<b>playerHealthTotal</b> - Health of all players in total.\n" +
                                     "<b>mousePosX</b> - Position X of the mouse cursor.\n" +
                                     "<b>mousePosY</b> - Position Y of the mouse cursor.\n" +
                                     "<b>screenWidth</b> - Width of the Application Window.\n" +
                                     "<b>screenHeight</b> - Height of the Application Window.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("If you have a few functions listed, follow this example:\n" +
                                    "clamp(random(), 0, 1) + clamp(random(034) * 0.1, 0, 1) * pitch"
                                    , EditorDocument.Element.Type.Text),
                new EditorDocument.Element("Below is a list of functions that can be used with math evaluation.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element(
                                    "<b>sin(value)</b> - sin function.\n" +
                                    "<b>cos(value)</b> - cos function.\n" +
                                    "<b>atan(value)</b> - atan function.\n" +
                                    "<b>tan(value)</b> - tan function.\n" +
                                    "<b>asin(value)</b> - asin function.\n" +
                                    "<b>sqrt(value)</b> - sqrt function.\n" +
                                    "<b>abs(value)</b> - abs function.\n" +
                                    "<b>min(a, b)</b> - Limits a to always be below b.\n" +
                                    "<b>max(a, b)</b> - Limits a to always be above b.\n" +
                                    "<b>clamp(value, min, max)</b> - Limits the value to always be between min and max.\n" +
                                    "<b>clampZero(value, min, max)</b> - If both min and max are zero, then it will not clamp.\n" +
                                    "<b>pow(f, p)</b> - pow function.\n" +
                                    "<b>exp(power)</b> - exponent function.\n" +
                                    "<b>log(f)</b> - log function.\n" +
                                    "<b>log(f, p)</b> - log function.\n" +
                                    "<b>log10(f)</b> - log function.\n" +
                                    "<b>ceil(f)</b> - ceiling function.\n" +
                                    "<b>floor(f)</b> - floor function.\n" +
                                    "<b>round(f)</b> - Rounds 'f' to the nearest whole number.\n" +
                                    "<b>sign(f)</b> - sign function.\n" +
                                    "<b>lerp(a, b, t)</b> - Interpolates between a (start) and b (end) depending on t (time).\n" +
                                    "<b>lerpAngle(a, b, t)</b> - Interpolates between a (start) and b (end) depending on t (time).\n" +
                                    "<b>inverseLerp(a, b, t)</b> - Interpolates between a (start) and b (end) depending on t (time).\n" +
                                    "<b>moveTowards(current, target, maxDelta)</b> - Moves current towards the target depending on maxDelta.\n" +
                                    "<b>moveTowardsAngle(current, target, maxDelta)</b> - Moves current towards the target depending on maxDelta.\n" +
                                    "<b>smoothStep(from, to, t)</b> - Like lerp, except smoothly interpolates.\n" +
                                    "<b>gamma(value, absmax, gamma)</b> - gamma function.\n" +
                                    "<b>approximately(a, b)</b> - Checks if a and b are almost or fully equal.\n" +
                                    "<b>repeat(t, length)</b> - Repeats t by length.\n" +
                                    "<b>pingPong(t, length)</b> - Ping-pongs between t by length.\n" +
                                    "<b>deltaAngle(current, target)</b> - Calculates an angle from current to target.\n" +
                                    "<b>random()</b> - Gets a random seed and returns a non-whole number by the seed.\n" +
                                    "<b>random(seed)</b> - Returns a non-whole number by the seed. Example: random(4104) will always result in the same random number.\n" +
                                    "<b>random(seed, index)</b> - Returns a consistent non-whole number by the seed and index.\n" +
                                    "<b>randomRange(seed, min, max)</b> - Returns a non-whole number between min and max by the seed.\n" +
                                    "<b>randomRange(seed, min, max, index)</b> - Returns a consistent non-whole number between min and max by the seed and index.\n" +
                                    "<b>randomInt()</b> - Gets a random seed and returns a whole number by the seed. Example: random(4104) will always result in the same random number.\n" +
                                    "<b>randomInt(seed)</b> - Returns a whole number by the seed.\n" +
                                    "<b>randomInt(seed, index)</b> - Returns a consistent whole number by the seed and index.\n" +
                                    "<b>randomRangeInt(seed, min, max)</b> - Returns a whole number between min and max by the seed.\n" +
                                    "<b>randomRangeInt(seed, min, max, index)</b> - Returns a consistent whole number between min and max by the seed and index.\n" +
                                    "<b>roundToNearestNumber(value, multipleOf)</b> - Rounds the value to the nearest 'multipleOf'.\n" +
                                    "<b>roundToNearestDecimal(value, places)</b> - Ensures a specific decimal length. E.g. value is 0.51514656 and places 3 would make the value 0.515.\n" +
                                    "<b>percentage(t, length)</b> - Calculates a percentage (100%) from t and length.\n" +
                                    "<b>equals(a, b, trueResult, falseResult)</b> - If a and b are equal, then the function returns the trueResult, otherwise returns the falseResult.\n" +
                                    "<b>lesserEquals(a, b, trueResult, falseResult)</b> - If a and b are lesser or equal, then the function returns the trueResult, otherwise returns the falseResult.\n" +
                                    "<b>greaterEquals(a, b, trueResult, falseResult)</b> - If a and b are greater or equal, then the function returns the trueResult, otherwise returns the falseResult.\n" +
                                    "<b>lesser(a, b, trueResult, falseResult)</b> - If a and b are lesser, then the function returns the trueResult, otherwise returns the falseResult.\n" +
                                    "<b>greater(a, b, trueResult, falseResult)</b> - If a and b are greater, then the function returns the trueResult, otherwise returns the falseResult.\n" +
                                    "<b>findAxis#object_group(int fromType, int fromAxis, float time)</b> - Finds an object with the matching tag, takes an axis from fromType and fromAxis and interpolates it via time.\n" +
                                    "<b>findOffset#object_group(int fromType, int fromAxis)</b> - Finds an object with the matching tag and takes an axis from fromType and fromAxis.\n" +
                                    "<b>findObject#object_group#Property()</b> - Finds an object with the matching tag and a property of the object.\n" +
                                    "Acceptible properties:\n" +
                                    "StartTime\n" +
                                    "Depth\n" +
                                    "IntVariable\n" +
                                    "<b>findInterpolateChain#object_group(float time, int type int axis, int includeDepth [0 = false 1 = true], int includeOffsets [0 = false 1 = true], int includeSelf [0 = false 1 = true])</b> - Finds an object with the matching tag and interpolates its full animation value. If type is not position aka type = 0, then don't have includeDepth in the parameters.\n" +
                                    "<b>easing#curveType(t)</b> - Calculates an easing from the easing list. \"curveType\" is easings such as \"Linear\", \"Instant\", etc.\n" +
                                    "<b>int(value)</b> - Casts floating point numbers (non-whole numbers) into integers (whole numbers).\n" +
                                    "<b>sampleAudio(int sample, float intensity)</b> - Takes a sample of the currently playing audio.\n" +
                                    "<b>vectorAngle(float firstX, float firstY, float firstZ, float secondX, float secondY, float secondZ)</b> - Calculates rotation where first would be looking at second.\n" +
                                    "<b>distance(float firstX, [optional] float firstY, float [optional] firstZ, float secondX, [optional] float secondY, [optional] float secondZ)</b> - Calculates the distance between first and second. Y and Z values are optional.\n" +
                                    "<b>date#format()</b> - Takes a specific part of the current date and uses it.\n" +
                                    "Acceptable formats:\n" +
                                    "yyyy = full year (e.g. 2019)\n" +
                                    "yy = decade year (e.g. 19)\n" +
                                    "MM = month (e.g. 06)\n" +
                                    "dd = day (e.g. 15)\n" +
                                    "HH = 24 hour (e.g. 13)\n" +
                                    "hh = 12 hour (e.g. 1)\n" +
                                    "mm = minute (e.g. 59)\n" +
                                    "ss = second (e.g. 12)\n" +
                                    "<b>mirrorNegative(float value)</b> - ensures the value is always a positive number. If it's lesser than 0, then it will reverse the number to positive. E.G: -4.2 to 4.2.\n" +
                                    "<b>mirrorPositive(float value)</b> - ensures the value is always a negative number. If it's greater than 0, then it will reverse the number to negative. E.G: 4.2 to -4.2.\n" +
                                    "<b>worldToViewportPointX(float x, [optional] float y, [optional] float z)</b> translates a position to the camera viewport and gets the X value. Y and Z values are optional.\n" +
                                    "<b>worldToViewportPointY(float x, [optional] float y, [optional] float z)</b> translates a position to the camera viewport and gets the Y value. Y and Z values are optional.\n" +
                                    "<b>worldToViewportPointZ(float x, [optional] float y, [optional] float z)</b> translates a position to the camera viewport and gets the Z value. Y and Z values are optional.\n", EditorDocument.Element.Type.Text),
            });

            //DateTime.Now.ToString("ff"); // yes
            //DateTime.Now.ToString("fff"); // yes
            //DateTime.Now.ToString("ffff"); // yes
            //DateTime.Now.ToString("fffff"); // yes
            //DateTime.Now.ToString("ffffff"); // yes
            //DateTime.Now.ToString("fffffff"); // yes
            //DateTime.Now.ToString("FF"); // yes
            //DateTime.Now.ToString("FFF"); // yes
            //DateTime.Now.ToString("FFFF"); // yes
            //DateTime.Now.ToString("FFFFF"); // yes
            //DateTime.Now.ToString("FFFFFF"); // yes
            //DateTime.Now.ToString("FFFFFFF"); // yes
            //DateTime.Now.ToString("yyyy"); // yes = year full
            //DateTime.Now.ToString("yy"); // yes = year decade
            //DateTime.Now.ToString("MM"); // yes = month
            //DateTime.Now.ToString("dd"); // yes = day
            //DateTime.Now.ToString("HH"); // yes = 24 hour
            //DateTime.Now.ToString("hh"); // yes = 12 hour
            //DateTime.Now.ToString("mm"); // yes = minute
            //DateTime.Now.ToString("ss"); // yes = second

            GenerateDocument("Misc (OUTDATED)", "The stuff that didn't fit in a document of its own.", new List<EditorDocument.Element>
            {
                new EditorDocument.Element("<b>Editor Level Path [MODDED]</b>\nThe path within the Project Arrhythmia/beatmaps directory that is used for the editor level list.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Refresh [MODDED]</b>\nRefreshes the editor level list.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Descending [MODDED]</b>\nIf the editor level list should be descending or ascending.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("<b>Order [MODDED]</b>\nHow the editor level list should be ordered." +
                    "\nCover - Order by if the level has a cover or not." +
                    "\nArtist - Order by Artist Name." +
                    "\nCreator - Order by Creator Name." +
                    "\nFolder - Order by Folder Name." +
                    "\nTitle - Order by Song Title." +
                    "\nDifficulty - Order by (Easy, Normal, Hard, Expert, Expert+, Master, Animation)" +
                    "\nDate Edited - Order by last saved time, so recently edited levels appear at one side and older levels appear at the other.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_open_level_top.png", EditorDocument.Element.Type.Image),
                new EditorDocument.Element("<b>Loading Autosaves [MODDED]</b>\nHolding shift when you click on a level in the level list will open an Autosave popup instead of " +
                    "loading the level. This allows you to load any autosaved file so you don't need to go into the level folder and change one of the autosaves to the level.lsb.", EditorDocument.Element.Type.Text),
                new EditorDocument.Element("BepInEx/plugins/Assets/Documentation/doc_autosaves.png", EditorDocument.Element.Type.Image),
            });

            if (CoreHelper.AprilFools)
            {
                var elements = new List<EditorDocument.Element>();

                elements.Add(new EditorDocument.Element("oops, i spilled my images everywhere...", EditorDocument.Element.Type.Text));

                var dir = Directory.GetFiles(RTFile.ApplicationDirectory, "*.png", SearchOption.AllDirectories);

                for (int i = 0; i < UnityEngine.Random.Range(0, Mathf.Clamp(dir.Length, 0, 20)); i++)
                    elements.Add(new EditorDocument.Element(dir[UnityEngine.Random.Range(0, dir.Length)].Replace("\\", "/").Replace(RTFile.ApplicationDirectory, ""), EditorDocument.Element.Type.Image));

                GenerateDocument("April Fools!", "fol.", elements);
            }

            try
            {
                Dialog = new EditorDialog(EditorDialog.DOCUMENTATION);
                Dialog.Init();
            }
            catch (Exception ex)
            {
                CoreHelper.LogException(ex);
            } // init dialog
        }

        #endregion

        #region Values

        public EditorDialog Dialog { get; set; }

        public List<EditorDocument> documents = new List<EditorDocument>();

        public Text documentationTitle;
        public string documentationSearch;
        public Transform documentationContent;

        #endregion

        #region Methods

        /// <summary>
        /// Refreshes the list of documents.
        /// </summary>
        public void RefreshDocumentation()
        {
            if (documents.Count > 0)
                foreach (var document in documents)
                {
                    var active = RTString.SearchString(documentationSearch, document.Name);
                    document.PopupButton?.SetActive(active);
                    if (active && document.PopupButton && document.PopupButton.TryGetComponent(out Button button))
                    {
                        button.onClick.ClearAll();
                        button.onClick.AddListener(() => OpenDocument(document));
                    }
                }
        }

        /// <summary>
        /// Searches for a documentation and shows it.
        /// </summary>
        /// <param name="name">The document name.</param>
        public void OpenDocument(string name)
        {
            if (documents.TryFind(x => x.Name == name, out EditorDocument document))
                OpenDocument(document);
        }

        /// <summary>
        /// Selects and opens the documentation.
        /// </summary>
        /// <param name="document">Document to open.</param>
        public void OpenDocument(EditorDocument document)
        {
            documentationTitle.text = $"- {document.Name} -";

            var font = FontManager.inst.allFontAssets["Inconsolata Variable"];

            LSHelpers.DeleteChildren(documentationContent);

            int num = 0;
            foreach (var element in document.elements)
            {
                switch (element.type)
                {
                    case EditorDocument.Element.Type.Text:
                        {
                            if (string.IsNullOrEmpty(element.Data))
                                break;

                            var gameObject = Creator.NewUIObject("element", documentationContent);
                            RectValues.FullAnchored.AnchoredPosition(1f, 0f).Pivot(0f, 1f)
                                .SizeDelta(722f, element.Autosize || element.Height == 0f ? (22f * LSText.WordWrap(element.Data, 67).Count) : element.Height).AssignToRectTransform(gameObject.transform.AsRT());

                            var horizontalLayoutGroup = gameObject.AddComponent<HorizontalLayoutGroup>();

                            horizontalLayoutGroup.childControlHeight = false;
                            horizontalLayoutGroup.childControlWidth = false;
                            horizontalLayoutGroup.childForceExpandWidth = false;
                            horizontalLayoutGroup.spacing = 8f;
                            var image = gameObject.AddComponent<Image>();

                            var labels = Creator.NewUIObject("label", gameObject.transform);
                            RectValues.FullAnchored.Pivot(0f, 1f)
                                .SizeDelta(722f, 22f).AssignToRectTransform(labels.transform.AsRT());

                            var labelsHLG = labels.AddComponent<HorizontalLayoutGroup>();

                            labelsHLG.childControlHeight = false;
                            labelsHLG.childControlWidth = false;
                            labelsHLG.spacing = 8f;

                            var label = Creator.NewUIObject("text", labels.transform);

                            var text = label.AddComponent<TextMeshProUGUI>();
                            text.font = font;
                            text.fontSize = 20;
                            text.enableWordWrapping = true;
                            text.text = element.Data;
                            EditorThemeManager.ApplyGraphic(text, ThemeGroup.Light_Text);

                            if (element.Function != null)
                            {
                                var button = gameObject.AddComponent<Button>();
                                button.onClick.AddListener(element.RunFunction);
                                button.image = image;
                                EditorThemeManager.ApplySelectable(button, ThemeGroup.List_Button_1);
                            }
                            else
                                EditorThemeManager.ApplyGraphic(image, ThemeGroup.List_Button_1_Normal, true);

                            RectValues.FullAnchored.Pivot(0f, 1f).SizeDelta(722f, 22f).AssignToRectTransform(label.transform.AsRT());

                            break;
                        }
                    case EditorDocument.Element.Type.Image:
                        {
                            if (string.IsNullOrEmpty(element.Data))
                                break;

                            var gameObject = Creator.NewUIObject("element", documentationContent);
                            RectValues.FullAnchored.AnchoredPosition(1f, -48f).Pivot(0f, 1f).SizeDelta(720f, 432f).AssignToRectTransform(gameObject.transform.AsRT());

                            var baseImage = gameObject.AddComponent<Image>();
                            var mask = gameObject.AddComponent<Mask>();

                            EditorThemeManager.ApplyGraphic(baseImage, ThemeGroup.List_Button_1_Normal, true);

                            var imageObject = Creator.NewUIObject("image", gameObject.transform);
                            RectValues.FullAnchored.AnchorMax(0f, 1f).AnchorMin(0f, 1f).Pivot(0f, 1f).SizeDelta(632f, 432f).AssignToRectTransform(imageObject.transform.AsRT());

                            var image = imageObject.AddComponent<Image>();
                            image.color = new Color(1f, 1f, 1f, 1f);

                            if (RTFile.FileExists($"{RTFile.ApplicationDirectory}{element.Data}"))
                                image.sprite = SpriteHelper.LoadSprite($"{RTFile.ApplicationDirectory}{element.Data}");
                            else
                                image.enabled = false;

                            if (image.sprite && image.sprite.texture)
                            {
                                var width = Mathf.Clamp(image.sprite.texture.width, 0, 718);
                                gameObject.transform.AsRT().sizeDelta = new Vector2(width, image.sprite.texture.height);
                                imageObject.transform.AsRT().sizeDelta = new Vector2(width, image.sprite.texture.height);
                            }

                            if (element.Function != null)
                                imageObject.AddComponent<Button>().onClick.AddListener(element.RunFunction);

                            break;
                        }
                }

                // Spacer
                if (num != document.elements.Count - 1)
                {
                    var spacer = Creator.NewUIObject("spacer", documentationContent);
                    spacer.transform.AsRT().sizeDelta = new Vector2(764f, 2f);
                    var image = spacer.AddComponent<Image>();
                    EditorThemeManager.ApplyGraphic(image, ThemeGroup.Light_Text, true);
                }
                num++;
            }

            Dialog.Open();
        }

        #region Internal

        EditorDocument.Element FontElement(EditorDocument.SupportType condition, string name, string desc) => new EditorDocument.Element($"<b>[{condition}]</b> <font={name}>{name}</font> ({name}) - {desc}", EditorDocument.Element.Type.Text, () =>
        {
            EditorManager.inst.DisplayNotification($"Copied font!", 2f, EditorManager.NotificationType.Success);
            LSText.CopyToClipboard($"<font={name}>");
        });

        EditorDocument.Element LinkElement(string text, string url) => new EditorDocument.Element(text, EditorDocument.Element.Type.Text, () => Application.OpenURL(url));

        EditorDocument GenerateDocument(string name, string description, List<EditorDocument.Element> elements)
        {
            var gameObject = EditorManager.inst.folderButtonPrefab.Duplicate(RTEditor.inst.DocumentationPopup.Content, "Document");
            var documentation = new EditorDocument(gameObject, name, description);

            EditorThemeManager.AddSelectable(gameObject.GetComponent<Button>(), ThemeGroup.List_Button_1);

            documentation.elements.AddRange(elements);

            gameObject.AddComponent<HoverTooltip>().tooltipLangauges.Add(new HoverTooltip.Tooltip { desc = name, hint = description });

            var text = gameObject.transform.GetChild(0).GetComponent<Text>();

            text.text = documentation.Name;
            EditorThemeManager.AddLightText(text);

            documents.Add(documentation);
            return documentation;
        }

        #endregion

        #endregion
    }
}
