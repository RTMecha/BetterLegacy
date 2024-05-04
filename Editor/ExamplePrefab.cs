using BetterLegacy.Core.Data;
using BetterLegacy.Editor.Managers;
using System.Collections.Generic;
using UnityEngine;
using AutoKillType = DataManager.GameData.BeatmapObject.AutoKillType;
using ObjectType = BetterLegacy.Core.Data.BeatmapObject.ObjectType;

namespace BetterLegacy.Editor
{
    public class ExamplePrefab : MonoBehaviour
    {
        static BeatmapObject GenerateTemplatePrefabs(string _name,
            ObjectType _objectType,
            Vector2 _origin,
            AutoKillType _autoKillType,
            float _autoKillOffset,
            int _depth,
            int _bin,
            Vector2 _pos,
            Vector2 _sca,
            float _rot,
            int _col,
            string _id, string _p,
            int _s, int _so,
            float _startTime,
            string _pt)
        {
            var bm = ObjectEditor.CreateNewBeatmapObject(0f, false);
            bm.name = _name;
            bm.objectType = _objectType;
            bm.origin = _origin;
            bm.autoKillType = _autoKillType;
            bm.autoKillOffset = _autoKillOffset;
            bm.Depth = _depth;
            bm.editorData.Bin = _bin;
            bm.events[0][0].eventValues[0] = _pos.x;
            bm.events[0][0].eventValues[1] = _pos.y;
            bm.events[1][0].eventValues[0] = _sca.x;
            bm.events[1][0].eventValues[1] = _sca.y;
            bm.events[2][0].eventValues[0] = _rot;
            bm.events[3][0].eventValues[0] = _col;
            bm.id = _id;
            bm.parent = _p;
            bm.shape = _s;
            bm.shapeOption = _so;
            bm.StartTime = _startTime;
            bm.parentType = _pt;

            return bm;
        }

        public static Prefab example;
        public static Prefab PAExampleM
        {
            get
            {
                if (example == null)
                    example = new Prefab
                    {
                        ID = "toYoutoYoutoYou",
                        Name = "PA Example M",
                        objects = new List<DataManager.GameData.BeatmapObject>
                        {
                            GenerateTemplatePrefabs("pupil shine example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 12, 1, new Vector2(-0.35f, 0.2f), new Vector2(0.4f, 0.6f), -30f, 7, "VS0R▉®‿▥®t)3Tœ8□", "HEœt■¶▦B1▼░Ã☳□%□", 1, 0, 0.09199905f, "111"),
                            GenerateTemplatePrefabs("pupil shine example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 12, 1, new Vector2(-0.35f, 0.2f), new Vector2(0.4f, 0.6f), -30f, 7, "^☷%&µmIHbKd8®¾œ:", "✿PZS▧èsqÿ▦▥u✿IJ⁕", 1, 0, 0.1849995f, "111"),
                            GenerateTemplatePrefabs("ear example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 17, 2, new Vector2(0f, 4f), new Vector2(10f, 4f), -90f, 0, "ÃGGò%q~8C|▐A}x◄_", "&}ò_2OO▨;DA►▤vRK", 1, 2, 0f, "111"),
                            GenerateTemplatePrefabs("ear example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 8, 2, Vector2.zero, new Vector2(4f, 4f), -90f, 0, "y▓▒¥kdYjj◠☰7m▨/▧", "m☶☷^+tK▨[☱‿F▦Pig", 1, 0, 0.09199905f, "111"),
                            GenerateTemplatePrefabs("ear example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 8, 2, Vector2.zero, new Vector2(10f, 4f), 90f, 0, "¾hfAT¤¤)èðD'JKWr", "m☶☷^+tK▨[☱‿F▦Pig", 1, 2, 0.1849995f, "111"),
                            GenerateTemplatePrefabs("ear example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 16, 2, new Vector2(0f, 4f), new Vector2(9f, 2f), -90f, 1, "US6VAXPv®D▧BL9|F", "&}ò_2OO▨;DA►▤vRK", 1, 2, 0.2769985f, "111"),
                            GenerateTemplatePrefabs("ear example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 17, 3, new Vector2(0f, 4f), new Vector2(10f, 4f), -90f, 0, "▥u}l■R|}0☰*cxÃ,>", "u+®p[Vl)/☲░2eLJ◠", 1, 2, 0f, "111"),
                            GenerateTemplatePrefabs("ear example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 8, 3, Vector2.zero, new Vector2(4f, 4f), -90f, 0, "uw▼▤*▦œè¾òs~N@>▉", "ÃW#*☷▦pb1H[bTœ*□", 1, 0, 0.09199905f, "111"),
                            GenerateTemplatePrefabs("ear example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 8, 3, Vector2.zero, new Vector2(10f, 4f), 90f, 0, "⁕ð▉▆tUS<œL_bG:☰M", "ÃW#*☷▦pb1H[bTœ*□", 1, 2, 0.1849995f, "111"),
                            GenerateTemplatePrefabs("ear example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 16, 3, new Vector2(0f, 4f), new Vector2(9f, 2f), -90f, 1, "☷DIœ0ÿ▢H2SO¥6_9x", "u+®p[Vl)/☲░2eLJ◠", 1, 2, 0.2769985f, "111"),
                            GenerateTemplatePrefabs("snout example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 10, 4, new Vector2(0f, -2.5f), new Vector2(4f, 6f), -90f, 1, "/ÿt¾/aXXR{s>Ã‿')", "NKo~uÃ+mX&œ▩XèsV", 1, 2, 0.09199905f, "111"),
                            GenerateTemplatePrefabs("snout example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 10, 4, new Vector2(0f, -2.5f), new Vector2(2f, 6f), 90f, 1, "c►b<m0▬h#E¥i□¥▦▨", "NKo~uÃ+mX&œ▩XèsV", 1, 2, 0.1849995f, "111"),
                            GenerateTemplatePrefabs("tail example", ObjectType.Helper, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 16, 5, new Vector2(0f, -0.4f), new Vector2(1.8f, 2.4f), -90f, 8, "&¤▦/¾(<▬2▉™V▉P▐a", "Ow□¤X▦▓®^*h|◠œ?☶", 1, 2, 0f, "111"),
                            GenerateTemplatePrefabs("tail example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 17, 5, new Vector2(0f, -0.5f), new Vector2(2.5f, 3f), 0f, 0, "6ðU░v▨¤ð▐☶w◄w}j☲", "Ow□¤X▦▓®^*h|◠œ?☶", 1, 0, 0.0929985f, "111"),
                            GenerateTemplatePrefabs("tail example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 17, 5, new Vector2(0f, -1.8f), new Vector2(2.3f, 2.5f), 180f, 0, "i►(*6j☴(0,v0¤□▥|", "Ow□¤X▦▓®^*h|◠œ?☶", 2, 0, 0.1849995f, "111"),
                            GenerateTemplatePrefabs("tail example", ObjectType.Helper, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 16, 5, new Vector2(0f, -0.4f), new Vector2(2.8f, 2.4f), 90f, 8, ">8KzD(<d☵<¤^(®u☳", "Ow□¤X▦▓®^*h|◠œ?☶", 1, 2, 0.2779999f, "111"),
                            GenerateTemplatePrefabs("nose curve example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 7, 6, new Vector2(-0.25f, 0.145f), new Vector2(1f, 0.1f), 60f, 8, "☶░▤.At{4okmF▩g™N", "▼,☳l▼7¾|Pa☱W2èS☳", 1, 0, 0.09199905f, "111"),
                            GenerateTemplatePrefabs("nose curve example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 7, 6, new Vector2(0.25f, 0.145f), new Vector2(1f, 0.1f), -60f, 8, "■nR☶◠^.(a*▣^▣▢▦▬", "▼,☳l▼7¾|Pa☱W2èS☳", 1, 0, 0.1849995f, "111"),
                            GenerateTemplatePrefabs("nose curve example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 7, 6, new Vector2(0f, -0.28f), new Vector2(1f, 0.5f), 0f, 8, "6EzJF¾p¤ÿ■è~qxµ☱", "▼,☳l▼7¾|Pa☱W2èS☳", 1, 0, 0.2769985f, "111"),
                            GenerateTemplatePrefabs("lip example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 9, 7, new Vector2(-0.875f, 0f), new Vector2(2f, 2f), -90f, 8, "4|I¾*e'☰☱Mð7<70^", "~R▧░][XS6I☴¾▩‿ß4", 1, 6, 0f, "111"),
                            GenerateTemplatePrefabs("lip example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 9, 7, new Vector2(0.875f, 0f), new Vector2(2f, 2f), -180f, 8, "L.F;]èIDm9oT)T◠ð", "~R▧░][XS6I☴¾▩‿ß4", 1, 6, 0.09199905f, "111"),
                            GenerateTemplatePrefabs("lip example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 8, 7, Vector2.zero, new Vector2(0.8f, 1f), 0f, 1, "m▢v4'p▉▨F▒xOOgb■", ";Xò2¶R]do¶NZ%pi&", 1, 0, 0.1849995f, "111"),
                            GenerateTemplatePrefabs("lip example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 8, 7, Vector2.zero, new Vector2(0.8f, 1f), 90f, 1, "☲Ã►oee_▧aQP◠L|░V", "►ÿfL☶:}djjuW2;7s", 1, 0, 0.2769985f, "111"),
                            GenerateTemplatePrefabs("hand example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 3, 8, Vector2.zero, new Vector2(4f, 4f), 0f, 1, "◠▩w/E▨*▼r▒▉@™!q☰", "ßð5]ð◠a☲i'ß@e☲¾U", 1, 0, 0.09199905f, "111"),
                            GenerateTemplatePrefabs("hand example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 3, 8, Vector2.zero, new Vector2(4f, 4f), 0f, 1, "▐~xR#pp9▣5$Y1▩r2", "✿)¾$w7µ□9I1▣òÃ☱☳", 1, 0, 0.1849995f, "111"),
                            GenerateTemplatePrefabs("head example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 15, 0, Vector2.zero, new Vector2(10f, 10f), 0f, 0, "es|l?▬2z▨i☶Z(06k", "{◄☷}^h}$N3xv7BTß", 1, 0, 0.9249992f, "111"),
                            GenerateTemplatePrefabs("brow left example", ObjectType.Decoration, new Vector2(-0.5f, 0f), AutoKillType.OldStyleNoAutokill, 0f, 10, 1, new Vector2(3f, 0f), new Vector2(2f, 0.7f), 0f, 8, "▼ng7?p*▧▬~✿H.IœF", "☷▓*snI,nBµ▧◄;☰c*", 1, 0, 0.9249992f, "111"),
                            GenerateTemplatePrefabs("brow right example", ObjectType.Decoration, new Vector2(0.5f, 0f), AutoKillType.OldStyleNoAutokill, 0f, 10, 2, new Vector2(-3f, 0f), new Vector2(2f, 0.7f), 0f, 8, "!w▓Pò▢um▐☷▨:sB▤@", "☷▓*snI,nBµ▧◄;☰c*", 1, 0, 0.9249992f, "111"),
                            GenerateTemplatePrefabs("eye left example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 14, 3, new Vector2(2f, 0f), new Vector2(3.5f, 3.5f), 0f, 7, "Ã☲Q▨h☶☲Ch☵Q8◄hè☶", "?☱1QtXu▥▩h+3r%^y", 1, 0, 0.9249992f, "111"),
                            GenerateTemplatePrefabs("eye right example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 14, 4, new Vector2(-2f, 0f), new Vector2(3.5f, 3.5f), 0f, 7, "ok☲dHKarW¤☴▆▩▤□{", "?☱1QtXu▥▩h+3r%^y", 1, 0, 0.9249992f, "111"),
                            GenerateTemplatePrefabs("pupil left example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 13, 5, new Vector2(1.8f, 0f), new Vector2(1.5f, 2.5f), 0f, 8, "HEœt■¶▦B1▼░Ã☳□%□", "!◠ß▨ED¶ly6LF'z▒_", 1, 0, 0.9249992f, "111"),
                            GenerateTemplatePrefabs("pupil right example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 13, 6, new Vector2(-1.8f, 0f), new Vector2(1.5f, 2.5f), 0f, 8, "✿PZS▧èsqÿ▦▥u✿IJ⁕", "!◠ß▨ED¶ly6LF'z▒_", 1, 0, 0.9249992f, "111"),
                            GenerateTemplatePrefabs("lid upper example", ObjectType.Decoration, new Vector2(-0.5f, 0f), AutoKillType.OldStyleNoAutokill, 0f, 11, 7, new Vector2(0f, 2.25f), new Vector2(0f, 7f), 90f, 0, "▥I(‿☲+T~P'+T|,▬▣", "?☱1QtXu▥▩h+3r%^y", 1, 2, 0.9249992f, "111"),
                            GenerateTemplatePrefabs("lid lower example", ObjectType.Decoration, new Vector2(-0.5f, 0f), AutoKillType.OldStyleNoAutokill, 0f, 11, 8, new Vector2(0f, -2.25f), new Vector2(0f, 7f), -90f, 0, "T✿tABa▨)4▐µ+>vGK", "?☱1QtXu▥▩h+3r%^y", 1, 2, 0.9249992f, "111"),
                            GenerateTemplatePrefabs("angery example", ObjectType.Decoration, new Vector2(0f, 0.5f), AutoKillType.OldStyleNoAutokill, 0f, 11, 9, new Vector2(0f, 2.5f), new Vector2(8f, 0f), 180f, 0, "pZq►PD}☱œJ2.kzPµ", "?☱1QtXu▥▩h+3r%^y", 2, 0, 0.9249992f, "111"),
                            GenerateTemplatePrefabs("nose example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 7, 10, new Vector2(0f, 0.5f), new Vector2(2f, 0.5f), -180f, 8, "▼,☳l▼7¾|Pa☱W2èS☳", "88')ðH¾G◠ß7▨r▦a▐", 2, 0, 0.9249992f, "111"),
                            GenerateTemplatePrefabs("lip left example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 9, 11, new Vector2(0.875f, 0f), new Vector2(2f, 2f), -120f, 8, ";Xò2¶R]do¶NZ%pi&", "~R▧░][XS6I☴¾▩‿ß4", 1, 6, 0.9249992f, "111"),
                            GenerateTemplatePrefabs("lip right example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 9, 12, new Vector2(-0.875f, 0f), new Vector2(2f, 2f), -150f, 8, "►ÿfL☶:}djjuW2;7s", "~R▧░][XS6I☴¾▩‿ß4", 1, 6, 0.9249992f, "111"),
                            GenerateTemplatePrefabs("mouth upper example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 9, 13, Vector2.zero, new Vector2(0.6f, 1f), 90f, 8, "z▥⁕☷▢G;<8▓M☵▦>%☱", "☷fWnß☴!☴q<y+l☷☶}", 1, 2, 0.9249992f, "111"),
                            GenerateTemplatePrefabs("mouth lower example", ObjectType.Decoration, Vector2.zero, AutoKillType.OldStyleNoAutokill, 0f, 9, 14, Vector2.zero, new Vector2(2f, 1f), -90f, 8, "6■™]4X&►q4►j#☰zi", "☷fWnß☴!☴q<y+l☷☶}", 1, 2, 0.9249992f, "111"),
                            GenerateTemplatePrefabs("Ears Origin P_Example", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframe, 5f, 0, 0, Vector2.zero, Vector2.one, 0f, 6, "✿fv®▦Cœt.▬▬☲(r☵0", "{◄☷}^h}$N3xv7BTß", 0, 0, 1.387999f, "111"),
                            GenerateTemplatePrefabs("Ear Left Origin P_Example", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframe, 5f, 0, 1, Vector2.zero, Vector2.one, -45f, 6, "EEf☴O‿]*™►dH(0iP", "✿fv®▦Cœt.▬▬☲(r☵0", 0, 0, 1.387999f, "111"),
                            GenerateTemplatePrefabs("Ear1 Left P_Example", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframe, 5f, 0, 2, new Vector2(0f, 4f), Vector2.one, 15f, 6, "&}ò_2OO▨;DA►▤vRK", "EEf☴O‿]*™►dH(0iP", 0, 0, 1.387999f, "111"),
                            GenerateTemplatePrefabs("Ear2 Left P_Example", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframe, 5f, 0, 3, new Vector2(0f, 4f), Vector2.one, -105f, 6, "m☶☷^+tK▨[☱‿F▦Pig", "&}ò_2OO▨;DA►▤vRK", 0, 0, 1.387999f, "111"),
                            GenerateTemplatePrefabs("Ear Right Origin P_Example", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframe, 5f, 0, 4, Vector2.zero, Vector2.one, 45f, 6, "[■n.uT☶N5$¤™M▦/A", "✿fv®▦Cœt.▬▬☲(r☵0", 0, 0, 1.387999f, "111"),
                            GenerateTemplatePrefabs("Ear1 Right P_Example", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframe, 5f, 0, 5, new Vector2(0f, 4f), Vector2.one, -15f, 6, "u+®p[Vl)/☲░2eLJ◠", "[■n.uT☶N5$¤™M▦/A", 0, 0, 1.387999f, "111"),
                            GenerateTemplatePrefabs("Ear2 Right P_Example", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframe, 5f, 0, 6, new Vector2(0f, 4f), Vector2.one, 105f, 6, "ÃW#*☷▦pb1H[bTœ*□", "u+®p[Vl)/☲░2eLJ◠", 0, 0, 1.387999f, "111"),
                            GenerateTemplatePrefabs("Tail <Origin> P_Example", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframe, 5f, 0, 7, Vector2.zero, Vector2.one, -15f, 6, "C{]ß?Cò.0&4✿☴™pi", "{◄☷}^h}$N3xv7BTß", 0, 0, 1.387999f, "111"),
                            GenerateTemplatePrefabs("Tail P_Example", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframe, 5f, 0, 8, new Vector2(0f, -4.5f), Vector2.one, -45f, 6, "Ow□¤X▦▓®^*h|◠œ?☶", "C{]ß?Cò.0&4✿☴™pi", 0, 0, 1.387999f, "111"),
                            GenerateTemplatePrefabs("Brow P_Example", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframe, 5f, 0, 0, new Vector2(0f, 3f), Vector2.one, 0f, 6, "☷▓*snI,nBµ▧◄;☰c*", "NKo~uÃ+mX&œ▩XèsV", 0, 0, 1.851f, "111"),
                            GenerateTemplatePrefabs("Eyes P_Example", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframe, 5f, 0, 1, Vector2.zero, Vector2.one, 0f, 6, "?☱1QtXu▥▩h+3r%^y", "NKo~uÃ+mX&œ▩XèsV", 0, 0, 1.851f, "111"),
                            GenerateTemplatePrefabs("Pupils P_Example", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframe, 5f, 0, 2, Vector2.zero, Vector2.one, 0f, 6, "!◠ß▨ED¶ly6LF'z▒_", "?☱1QtXu▥▩h+3r%^y", 0, 0, 1.851f, "101"),
                            GenerateTemplatePrefabs("Snout P_Example", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframe, 5f, 0, 3, new Vector2(0f, -2.5f), Vector2.one, 0f, 6, "88')ðH¾G◠ß7▨r▦a▐", "NKo~uÃ+mX&œ▩XèsV", 0, 0, 1.851f, "111"),
                            GenerateTemplatePrefabs("m-Mouth P_Example", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframe, 5f, 0, 4, new Vector2(0f, -0.5f), Vector2.one, 0f, 6, "LÿgVUrdGQ{_vL■%p", "88')ðH¾G◠ß7▨r▦a▐", 0, 0, 1.851f, "111"),
                            GenerateTemplatePrefabs("Lips P_Example", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframe, 5f, 0, 5, new Vector2(0f, 0.5f), Vector2.one, 0f, 6, "~R▧░][XS6I☴¾▩‿ß4", "LÿgVUrdGQ{_vL■%p", 0, 0, 1.851f, "111"),
                            GenerateTemplatePrefabs("Mouth P_Example", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframe, 5f, 0, 6, Vector2.zero, new Vector2(3f, 1f), 0f, 6, "☷fWnß☴!☴q<y+l☷☶}", "LÿgVUrdGQ{_vL■%p", 0, 0, 1.851f, "111"),
                            GenerateTemplatePrefabs("P_Example X", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframeOffset, 5f, 0, 0, Vector2.zero, Vector2.one, 0f, 6, "M▓☳6P☱Hÿ▨✿!☲R>W.", "", 0, 0, 2.313999f, "101"),
                            GenerateTemplatePrefabs("P_Example Y", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframeOffset, 5f, 0, 1, Vector2.zero, Vector2.one, 0f, 6, ",?◠~▆▉dc%▓wFmß☶⁕", "M▓☳6P☱Hÿ▨✿!☲R>W.", 0, 0, 2.313999f, "111"),
                            GenerateTemplatePrefabs("P_Example Rotscale", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframeOffset, 5f, 0, 2, Vector2.zero, Vector2.one, 0f, 6, "¥C™8jd◄/◄►☰U¤▧,▤", ",?◠~▆▉dc%▓wFmß☶⁕", 0, 0, 2.313999f, "111"),
                            GenerateTemplatePrefabs("P_Example Head X", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframeOffset, 5f, 0, 4, Vector2.zero, Vector2.one, 0f, 6, "☴▩▧m0T%1☶x¤A:k▆$", "¥C™8jd◄/◄►☰U¤▧,▤", 0, 0, 2.313999f, "111"),
                            GenerateTemplatePrefabs("P_Example Head Y", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframeOffset, 5f, 0, 5, Vector2.zero, Vector2.one, 0f, 6, "{◄☷}^h}$N3xv7BTß", "☴▩▧m0T%1☶x¤A:k▆$", 0, 0, 2.313999f, "111"),
                            GenerateTemplatePrefabs("P_Example Face X", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframeOffset, 5f, 0, 6, Vector2.zero, Vector2.one, 0f, 6, "zmc¶EXSgvv►Ã☶mO◠", "{◄☷}^h}$N3xv7BTß", 0, 0, 2.313999f, "111"),
                            GenerateTemplatePrefabs("P_Example Face Y", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframeOffset, 5f, 0, 7, Vector2.zero, Vector2.one, 0f, 6, "NKo~uÃ+mX&œ▩XèsV", "zmc¶EXSgvv►Ã☶mO◠", 0, 0, 2.313999f, "111"),
                            GenerateTemplatePrefabs("P_Example Hands <Origin>", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframeOffset, 5f, 0, 9, Vector2.zero, Vector2.one, 0f, 6, "U►☴.jÿ▥z&#f▧▒H▬▒", "¥C™8jd◄/◄►☰U¤▧,▤", 0, 0, 2.313999f, "111"),
                            GenerateTemplatePrefabs("P_Example Left Hand X", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframeOffset, 5f, 0, 10, new Vector2(6f, 0f), Vector2.one, 0f, 6, "}▆Vÿ‿░Ub9<☶*8B™!", "U►☴.jÿ▥z&#f▧▒H▬▒", 0, 0, 2.313999f, "111"),
                            GenerateTemplatePrefabs("P_Example Left Hand Y", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframeOffset, 5f, 0, 11, new Vector2(0f, -7f), Vector2.one, 0f, 6, "ßð5]ð◠a☲i'ß@e☲¾U", "}▆Vÿ‿░Ub9<☶*8B™!", 0, 0, 2.313999f, "111"),
                            GenerateTemplatePrefabs("P_Example Right Hand X", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframeOffset, 5f, 0, 12, new Vector2(-6f, 0f), Vector2.one, 0f, 6, "¾D!v▣|ZRdZ▥jx*t6", "U►☴.jÿ▥z&#f▧▒H▬▒", 0, 0, 2.313999f, "111"),
                            GenerateTemplatePrefabs("P_Example Right Hand Y", ObjectType.Empty, Vector2.zero, AutoKillType.LastKeyframeOffset, 5f, 0, 13, new Vector2(0f, -7f), Vector2.one, 0f, 6, "✿)¾$w7µ□9I1▣òÃ☱☳", "¾D!v▣|ZRdZ▥jx*t6", 0, 0, 2.313999f, "111"),
                        },
                        Offset = -2.31f,
                        Type = 5,
                        description = "Use me for anything!"
                    };
                return example;
            }
        }
    }
}
