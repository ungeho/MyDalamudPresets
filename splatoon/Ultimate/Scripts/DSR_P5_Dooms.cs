using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Hooks;
using ECommons.Hooks.ActionEffectTypes;
using ECommons.Logging;
using ImGuiNET;
using Microsoft.VisualBasic.ApplicationServices;
using Splatoon;
using Splatoon.SplatoonScripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;


namespace Scripts
{
    public class DSR_P5_Dooms : Scripts
    {
        // エリア(絶竜詩)
        public override HashSet<uint> ValidTerritories => new() { 968 };
        public override Metadata? Metadata => new(5, "Ungeho");

        List<Element> DoomElements = new();
        List<Element> NoDoomElements = new();
        Dictionary<double, IPlayerCharacter> plrs = new();

        //プレステは不要
        // Element? Circle1Element;
        // Element? Circle2Element;
        // Element? DoomSquareElement;
        // Element? DoomTriangleElement;
        // Element? NoDoomSquareElement;
        // Element? NoDoomTriangleElement;
        // Element? X1Element;
        // Element? X2Element;

        int count = 0;
        bool active = false;

        // ゲリックのデータID
        const uint GuerriqueDataId = 12637;
        bool positionDynamic = true;

        //IBattleNpc? Thordan => Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.DataId == ThordanDataId) as IBattleNpc;
        string TestOverride = "";

        //プレイヤーの取得？
        IPlayerCharacter PC => TestOverride != "" && FakeParty.Get().FirstOrDefault(x => x.Name.ToString() == TestOverride) is IPlayerCharacter pc ? pc : Svc.ClientState.LocalPlayer!;

        //MAPの中心座標
        Vector2 Center = new(100, 100);

        // 設定項目
        public override void OnSetup()
        {

            //プレステ部分は不要な為、コメントアウト

            // エレメント（プレステ表示）の設定
            // var circle1 = "{\"Name\":\"Doom Circle 1\",\"type\":1,\"Enabled\":false,\"offX\":2.0,\"offY\":9.0,\"radius\":0.7,\"Filled\":false,\"fillIntensity\":0.5,\"overlayBGColor\":0,\"overlayFScale\":1.7,\"thicc\":6.0,\"refActorNPCNameID\":3641,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}";
            // var circle2 = "{\"Name\":\"Doom Circle 2\",\"type\":1,\"Enabled\":false,\"offX\":-2.0,\"offY\":9.0,\"radius\":0.7,\"Filled\":false,\"fillIntensity\":0.5,\"overlayBGColor\":0,\"overlayFScale\":1.7,\"thicc\":6.0,\"refActorNPCNameID\":3641,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}";
            // var doomsquare = "{\"Name\":\"Doom Square\",\"Enabled\":false,\"type\":1,\"offX\":1.95,\"offY\":10.9,\"radius\":0.5,\"overlayBGColor\":0,\"overlayFScale\":1.7,\"thicc\":6.0,\"overlayText\":\"\",\"refActorNPCNameID\":3641,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"tether\":true,\"Filled\":true}";
            // var doomtriangle = "{\"Name\":\"Doom Triangle\",\"Enabled\":false,\"type\":1,\"offX\":-1.95,\"offY\":10.9,\"radius\":0.5,\"overlayBGColor\":0,\"overlayFScale\":1.7,\"thicc\":6.0,\"overlayText\":\"\",\"refActorNPCNameID\":3641,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"tether\":true,\"Filled\":true}";
            // var nodoomsquare = "{ \"Name\":\"Non doom Square\",\"Enabled\":false,\"type\":1,\"offX\":-1.92,\"offY\":7.15,\"radius\":0.5,\"color\":3372154884,\"overlayBGColor\":0,\"overlayFScale\":1.7,\"thicc\":6.0,\"overlayText\":\"\",\"refActorNPCNameID\":3641,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"tether\":true,\"Filled\":true}";
            // var nodoomtriangle = "{\"Name\":\"Non doom Triangle\",\"Enabled\":false,\"type\":1,\"offX\":1.95,\"offY\":7.15,\"radius\":0.5,\"color\":3372154884,\"overlayBGColor\":0,\"overlayFScale\":1.7,\"thicc\":6.0,\"overlayText\":\"\",\"refActorNPCNameID\":3641,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"tether\":true,\"Filled\":true}";
            // var nodoomx1 = "{\"Name\":\"Non doom X 1\",\"Enabled\":false,\"type\":1,\"offY\":6.12,\"radius\":0.5,\"color\":3372154884,\"overlayBGColor\":0,\"overlayFScale\":1.7,\"thicc\":6.0,\"overlayText\":\"N\",\"refActorNPCNameID\":3641,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"tether\":true,\"Filled\":true}";
            // var nodoomx2 = "{\"Name\":\"Non doom X 2\",\"Enabled\":false,\"type\":1,\"offY\":11.94,\"radius\":0.5,\"color\":3372154884,\"overlayBGColor\":0,\"overlayFScale\":1.7,\"thicc\":6.0,\"overlayText\":\"S\",\"refActorNPCNameID\":3641,\"refActorComparisonType\":6,\"includeRotation\":true,\"onlyUnTargetable\":true,\"tether\":true,\"Filled\":true}";
            //表示設定の登録(プレステ)
            //ここで登録された要素は、編集可能な項目(Registered Elements)に表示され、ユーザーが変更する事も出来る。
            // Circle1Element = Controller.RegisterElementFromCode($"circle1", circle1);
            // Circle2Element = Controller.RegisterElementFromCode($"circle2", circle2);
            // DoomSquareElement = Controller.RegisterElementFromCode($"doomsquare", doomsquare);
            // DoomTriangleElement = Controller.RegisterElementFromCode($"doomtriangle", doomtriangle);
            // NoDoomSquareElement = Controller.RegisterElementFromCode($"nodoomsquare", nodoomsquare);
            // NoDoomTriangleElement = Controller.RegisterElementFromCode($"nodoomtriangle", nodoomtriangle);
            // X1Element = Controller.RegisterElementFromCode($"x1", nodoomx1);
            // X2Element = Controller.RegisterElementFromCode($"x2", nodoomx2);

            //宣告持ちと無職の設定（主に、文字色 OverlayBGColor）
            var doom = "{\"Name\":\"\",\"radius\":0.0,\"overlayBGColor\":3365011624,\"overlayTextColor\":4294967295,\"overlayVOffset\":0.5,\"overlayFScale\":3.0,\"thicc\":0.0,\"overlayText\":\"1\",\"refActorType\":1}";
            var nodoom = "{\"Name\":\"\",\"radius\":0.0,\"overlayBGColor\":3372215296,\"overlayTextColor\":4294967295,\"overlayVOffset\":0.5,\"overlayFScale\":3.0,\"thicc\":0.0,\"overlayText\":\"1\",\"refActorType\":1}";
            for (var i = 0; i < 4; i++)
            {
                // 宣告持ちの設定を登録
                var e = Controller.RegisterElementFromCode($"doom{i}", doom);
                // 番号振り（優先度）
                e.overlayText = $"{i + 1}";
                // ユーザー設定の取得
                e.overlayBGColor = Conf.ColDoom.ToUint();
                e.offZ = Conf.offZ;
                e.overlayFScale = Conf.tScale;
                e.Enabled = false;
                DoomElements.Add(e);
            }
            for (var i = 0; i < 4; i++)
            {
                // 無職の設定を登録
                var e = Controller.RegisterElementFromCode($"nodoom{i}", nodoom);
                // 番号振り（優先度）
                e.overlayText = $"{i + 1}";
                // ユーザー設定の取得
                e.overlayBGColor = Conf.ColNoDoom.ToUint();
                e.offZ = Conf.offZ;
                e.overlayFScale = Conf.tScale;
                e.Enabled = false;
                NoDoomElements.Add(e);
            }
        }
        // Enableの時
        public override void OnEnable()
        {
            ActionEffect.ActionEffectEvent += ActionEffect_ActionEffectEvent;
        }

        //リリドではtriとsquを無職が調整する関係上、cirと×以外の場所を決定出来ない為、プレステ部分は表示しない。
        //VFX関連
        public override void OnVFXSpawn(uint target, string vfxPath)
        {
            // // 〇
            // if (vfxPath == "vfx/lockon/eff/r1fz_firechain_01x.avfx")
            // {
            //     DeactivateDoomMarkers();
            //     if (target.TryGetObject(out var pv) && pv is IPlayerCharacter pvc)
            //     {
            //         //DuoLog.Information($"{pvc.Name} has circle");
            //         if (pvc != PC)
            //             return;
            //         Circle1Element.Enabled = true;
            //         Circle2Element.Enabled = true;
            //     }
            // // △
            // } else if (vfxPath == "vfx/lockon/eff/r1fz_firechain_02x.avfx")
            // {
            //     if (target.TryGetObject(out var pv) && pv is IPlayerCharacter pvc)
            //     {
            //         //DuoLog.Information($"{pvc.Name} has triangle");
            //         if (pvc != PC)
            //             return;
            //         var doom = PC.StatusList.Where(z => z.StatusId == 2976);
            //         if (doom.Count() > 0)
            //         {
            //             DoomTriangleElement.Enabled = true;
            //         } else
            //         {
            //             NoDoomTriangleElement.Enabled = true;
            //         }
            //     }
            // // □
            // } else if (vfxPath == "vfx/lockon/eff/r1fz_firechain_03x.avfx")
            // {
            //     if (target.TryGetObject(out var pv) && pv is IPlayerCharacter pvc)
            //     {
            //         //DuoLog.Information($"{pvc.Name} has square");
            //         if (pvc != PC)
            //             return;
            //         var doom = PC.StatusList.Where(z => z.StatusId == 2976);
            //         if (doom.Count() > 0)
            //         {
            //             DoomSquareElement.Enabled = true;
            //         }
            //         else
            //         {
            //             NoDoomSquareElement.Enabled = true;
            //         }
            //     }
            // // X
            // } else if (vfxPath == "vfx/lockon/eff/r1fz_firechain_04x.avfx")
            // {
            //     if (target.TryGetObject(out var pv) && pv is IPlayerCharacter pvc)
            //     {
            //         //DuoLog.Information($"{pvc.Name} has x");
            //         if (pvc != PC)
            //             return;
            //         X1Element.Enabled = true;
            //         X2Element.Enabled = true;
            //     }
            // }
        }

        public override void OnMessage(string Message)
        {
            // 技の発動と紐づけされたメッセージが表示されたら
            if (Message.Contains("(3641>25557)"))
            {
                if (count == 0)
                {
                    count++;
                    return;
                }
                //DuoLog.Information($"Congaline should be complete now.");
                var guerrique = Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.DataId == GuerriqueDataId) as IBattleNpc;
                //DuoLog.Information($"Guerrique is at {guerrique.Position.X}/{guerrique.Position.Z}/{guerrique.Position.Y}, need to rotate {-guerrique.Rotation}");

                //PTリストの取得?
                var players = FakeParty.Get();
                // 宣告持ち
                int blue = 0;
                // 無職
                int red = 0;
                foreach (var p in players)
                {
                    //DuoLog.Information($"{p.Name} is @ {p.Position.X}/{p.Position.Z}/{p.Position.Y}");
                    //プレイヤーのXY座標から、ゲリック基準の位置を計算する。
                    plrs.Add(p.Position.X * Math.Cos(-guerrique.Rotation) + p.Position.Z * Math.Sin(-guerrique.Rotation), p);
                    var doom = p.StatusList.Where(z => z.StatusId == 2976);
                    //DuoLog.Information($"has {doom.Count()} dooms ");
                }
                //ゲリックとの相対的な位置から、重みづけされた要素を基準に、宣告と無職の番号を振る
                //つまり、優先度によって並び替えられた番号を振る。
                foreach (var p in plrs.OrderBy(x => x.Key))
                {
                    //DuoLog.Information($"{p.Value.Name}");
                    var doom = p.Value.StatusList.Where(z => z.StatusId == 2976);
                    if (doom.Count() > 0)
                    {
                        var e = DoomElements[red];
                        e.SetRefPosition(p.Value.Position);
                        e.Enabled = true;
                        red++;
                    }
                    else
                    {
                        var e = NoDoomElements[blue];
                        e.SetRefPosition(p.Value.Position);
                        e.Enabled = true;
                        blue++;
                    }
                }
                active = true;
            }

            //プレステ関連の表示を消すものなので、不要な為コメントアウト

            // 特定のメッセージが表示されたらプレステ散開位置の表示を消す。
            // if(Message.Contains("Ser Grinnaux uses Faith Unmoving.") | Message.Contains("聖騎士グリノーの「フェイスアンムーブ」"))
            // {
            //     DeactivateKnockbackMarkers();
            // }
        }

        //宣告,無職の表示削除
        private void DeactivateDoomMarkers()
        {
            NoDoomElements.Each(x => x.Enabled = false);
            DoomElements.Each(x => x.Enabled = false);
            active = false;
        }

        //プレステ散開の表示を削除
        private void DeactivateKnockbackMarkers()
        {
            // Circle1Element.Enabled = false;
            // Circle2Element.Enabled = false;
            // DoomSquareElement.Enabled = false;
            // DoomTriangleElement.Enabled = false;
            // NoDoomSquareElement.Enabled = false;
            // NoDoomTriangleElement.Enabled = false;
            // X1Element.Enabled = false;
            // X2Element.Enabled = false;
        }

        private void ActionEffect_ActionEffectEvent(ActionEffectSet set)
        {
            /*
            if (set.Action == null) return;
            if (set.Action.RowId == 25544)
            {
                //DuoLog.Information($"Position locked!");
                positionDynamic = false;
                for (var i = 0; i < Cones.Count; ++i)
                {
                    var c = Cones[i];
                    var e = ConeElements[i];
                    e.color = C.Col2.ToUint();
                    c.DelTime = Environment.TickCount64 + 2*1000;
                }
                //DuoLog.Information($"Thordan is @ {Thordan.Position.X}/{Thordan.Position.Z}/{Thordan.Position.Y}");
            }*/
        }

        //disableになった場合のトグル
        public override void OnDisable()
        {
            ActionEffect.ActionEffectEvent -= ActionEffect_ActionEffectEvent;
        }

        void Hide()
        {
        }

        // offの場合の初期化
        void Off()
        {
            plrs.Clear();
            count = 0;
            DeactivateDoomMarkers();
        }

        public override void OnUpdate()
        {
            // active出ない場合、ここは実行しない。
            if (!active)
                return;
            int blue = 0;
            int red = 0;
            foreach (var p in plrs.OrderBy(x => x.Key))
            {
                var doom = p.Value.StatusList.Where(z => z.StatusId == 2976);
                if (doom.Count() > 0)
                {
                    var e = DoomElements[red];
                    e.SetRefPosition(p.Value.Position);
                    red++;
                }
                else
                {
                    var e = NoDoomElements[blue];
                    e.SetRefPosition(p.Value.Position);
                    blue++;
                }
            }
        }

        public override void OnDirectorUpdate(DirectorUpdateCategory category)
        {
            //戦闘開始？、戦闘再開？、ワイプの時にOFF（初期化）にする。
            if (category.EqualsAny(DirectorUpdateCategory.Commence, DirectorUpdateCategory.Recommence, DirectorUpdateCategory.Wipe))
            {
                Off();
            }
        }

        Config Conf => Controller.GetConfig<Config>();

        public class Config : IEzConfig
        {
            //無職のオーバーレイテキスト背景色の初期設定
            public Vector4 ColNoDoom = Vector4FromRGBA(0x000000C8);
            //宣告持ちのオーバーレイテキスト背景色の初期設定
            public Vector4 ColDoom = Vector4FromRGBA(0x000000C8);
            //無職、宣告持ちの高さ(Vertical offset)設定
            public float offZ = 0.5f;
            //無職、宣告持ちのオーバーレイテキストの大きさ設定
            public float tScale = 3f;
        }


        //描画設定（ユーザーが変更できる項目の設置）
        public override void OnSettingsDraw()
        {
            //項目名と、無職、死の宣告の番号の背景色を設定出来る項目を設置
            ImGui.ColorEdit4("Non Doom Color", ref Conf.ColNoDoom, ImGuiColorEditFlags.NoInputs);
            ImGui.ColorEdit4("Doom Color", ref Conf.ColDoom, ImGuiColorEditFlags.NoInputs);
            // 罫線
            ImGui.Separator();
            // 余白
            ImGui.SetNextItemWidth(150);
            // 無職と宣告の数字の表示位置の高さ "項目名" (最小,最大),刻み幅);
            ImGui.DragFloat("Number vertical offset", ref Conf.offZ.ValidateRange(-5f, 5f), 0.1f);
            // 余白
            ImGui.SetNextItemWidth(150);
            //無職と宣告の数字の大きさをスライド式で変更できる項目を設定。"項目名" (最小,最大),刻み幅);
            ImGui.DragFloat("Number scale", ref Conf.tScale.ValidateRange(0.1f, 10f), 0.1f);
        }

        //色？
        public unsafe static Vector4 Vector4FromRGBA(uint col)
        {
            byte* bytes = (byte*)&col;
            return new Vector4((float)bytes[3] / 255f, (float)bytes[2] / 255f, (float)bytes[1] / 255f, (float)bytes[0] / 255f);
        }
    }
}