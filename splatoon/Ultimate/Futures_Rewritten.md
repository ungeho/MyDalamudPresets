# Futures Rewritten  


調整中  

最終更新日：----  

## Script  

## P1 Cyclonic Break  

公式から、サイクロニックブレイクのスクリプト  
設定不要  

```
https://raw.githubusercontent.com/PunishXIV/Splatoon/refs/heads/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P1%20Cyclonic%20Break.cs
```

## P1 Fall of Faith  

公式のシンソイルセヴァーのスクリプトの移動先の座標を、タゲサ上になるように変更したもの。  
要設定  

```
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons;
using ECommons.Configuration;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.PartyFunctions;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public class P1_Fall_of_Faith : SplatoonScript
{
    public enum Direction
    {
        North,
        East,
        South,
        West
    }

    private readonly ImGuiEx.RealtimeDragDrop<Job> DragDrop = new("DragDropJob", x => x.ToString());

    private Dictionary<string, PlayerData> _partyDatas = new();

    private State _state = State.None;

    private int _tetherCount = 1;
    public override HashSet<uint>? ValidTerritories => [1238];
    public override Metadata? Metadata => new(2, "Garume");
    private Config C => Controller.GetConfig<Config>();

    public override void OnStartingCast(uint source, uint castId)
    {
        if (_state == State.None && castId is 40137 or 40140)
        {
            _state = State.Start;
            var hasDebuffPlayer = FakeParty.Get().First(x => x.StatusList.Any(x => x.StatusId == 1051));
            if (castId == 40137)
                _partyDatas[hasDebuffPlayer.Name.ToString()] = new PlayerData(Debuff.Red, C.Tether1Direction, 1);
            else
                _partyDatas[hasDebuffPlayer.Name.ToString()] = new PlayerData(Debuff.Blue, C.Tether1Direction, 1);
            
            ApplyElement();
        }

        if (_state == State.Split && castId == 40170) _state = State.End;
    }


    public override void OnSetup()
    {
        for (var i = 0; i < 4; i++)
        {
            var element = new Element(1)
            {
                overlayVOffset = 3f,
                overlayFScale = 2f
            };
            Controller.RegisterElement("Tether" + i, element);
        }

        var bait = new Element(0)
        {
            thicc = 6f,
            tether = true
        };

        Controller.RegisterElement("Bait", bait);
    }

    private Vector2 GetPosition(Direction direction)
    {
        return direction switch
        {
            Direction.North => new Vector2(100, 95),
            Direction.East => new Vector2(105, 100),
            Direction.South => new Vector2(100, 105),
            Direction.West => new Vector2(95, 100),
            _ => Vector2.Zero
        };
    }

    public override void OnReset()
    {
        _state = State.None;
        _partyDatas = new Dictionary<string, PlayerData>();
        _tetherCount = 1;
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (_state != State.Start) return;
        _tetherCount++;
        if (_tetherCount is > 4 or < 2) return;
        if (target.GetObject() is not IPlayerCharacter targetPlayer) return;
        var name = targetPlayer.Name.ToString();

        var debuff = data3 switch
        {
            249 => Debuff.Red,
            287 => Debuff.Blue,
            _ => Debuff.None
        };
        _partyDatas[name] = _tetherCount switch
        {
            1 => new PlayerData(debuff, C.Tether1Direction, 1),
            2 => new PlayerData(debuff, C.Tether2Direction, 2),
            3 => new PlayerData(debuff, C.Tether3Direction, 3),
            4 => new PlayerData(debuff, C.Tether4Direction, 4),
            _ => _partyDatas[name]
        };

        if (_tetherCount == 4)
        {
            _state = State.Split;
            var noTether = C.PriorityData.GetPlayers(x => !_partyDatas.ContainsKey(x.Name));
            if (noTether == null)
            {
                DuoLog.Warning("[P1 Fall of Faith] NoTether is null");
                return;
            }
            _partyDatas[noTether[0].Name] = new PlayerData(Debuff.None, C.NoTether12Direction, 0);
            _partyDatas[noTether[1].Name] = new PlayerData(Debuff.None, C.NoTether12Direction, 0);
            _partyDatas[noTether[2].Name] = new PlayerData(Debuff.None, C.NoTether34Direction, 0);
            _partyDatas[noTether[3].Name] = new PlayerData(Debuff.None, C.NoTether34Direction, 0);
        }
        
        ApplyElement();
    }

    private void ApplyElement()
    {
        if (_partyDatas.TryGetValue(Player.Name, out var value) && Controller.TryGetElementByName("Bait", out var bait))
        {
            bait.Enabled = true;
            bait.SetOffPosition(GetPosition(value.Direction).ToVector3());
        }

        var index = 0;
        foreach (var data in _partyDatas.Where(x => x.Value.Debuff != Debuff.None))
        {
            var text = data.Value.Debuff switch
            {
                Debuff.Red => C.RedTetherText.Get() + data.Value.Count,
                Debuff.Blue => C.BlueTetherText.Get() + data.Value.Count,
                _ => string.Empty
            };

            if (Controller.TryGetElementByName("Tether" + index, out var tether))
            {
                tether.Enabled = true;
                tether.refActorName = data.Key;
                tether.overlayText = text;
            }

            index++;
        }
    }

    public override void OnUpdate()
    {
        switch (_state)
        {
            case State.None or State.End:
                Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
                break;
            case State.Start or State.Split:
            {
                if (Controller.TryGetElementByName("Bait", out var bait))
                    bait.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
                break;
            }
        }
    }

    public override void OnSettingsDraw()
    {
        ImGui.Text("General");

        ImGuiEx.EnumCombo("Tether1Direction##Tether1", ref C.Tether1Direction);
        ImGuiEx.EnumCombo("Tether2Direction##Tether2", ref C.Tether2Direction);
        ImGuiEx.EnumCombo("Tether3Direction##Tether1", ref C.Tether3Direction);
        ImGuiEx.EnumCombo("Tether4Direction##Tether1", ref C.Tether4Direction);
        ImGuiEx.EnumCombo("NoTether12Direction##NoTether12", ref C.NoTether12Direction);
        ImGuiEx.EnumCombo("NoTether34Direction##NoTether34", ref C.NoTether34Direction);

        ImGui.Separator();

        C.PriorityData.Draw();

        ImGui.Separator();

        ImGui.Text("RedTetherText:");
        ImGui.SameLine();
        var redTether = C.RedTetherText.Get();
        C.RedTetherText.ImGuiEdit(ref redTether);

        ImGui.Text("BlueTetherText:");
        ImGui.SameLine();
        var blueTether = C.BlueTetherText.Get();
        C.BlueTetherText.ImGuiEdit(ref blueTether);

        if (ImGui.CollapsingHeader("Debug"))
        {
            ImGui.Text("PartyDirection");
            ImGui.Indent();
            foreach (var (key, value) in _partyDatas) ImGui.Text($"{key} : {value}");
            ImGui.Unindent();

            ImGui.Text($"State: {_state}");
            ImGui.Text($"TetherCount: {_tetherCount}");
        }
    }

    private enum Debuff
    {
        None,
        Red,
        Blue
    }

    private enum State
    {
        None,
        Start,
        Split,
        End
    }

    private record PlayerData(Debuff Debuff, Direction Direction, int Count);


    public class Config : IEzConfig
    {
        public readonly Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public readonly Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();

        public InternationalString BlueTetherText = new() { Jp = "雷 散開" };

        public Direction NoTether12Direction = Direction.North;
        public Direction NoTether34Direction = Direction.South;

        public PriorityData PriorityData = new();

        public InternationalString RedTetherText = new() { Jp = "炎 ペア割" };

        public Direction Tether1Direction = Direction.North;
        public Direction Tether2Direction = Direction.South;
        public Direction Tether3Direction = Direction.North;
        public Direction Tether4Direction = Direction.South;
    }
}
```

* スクリプトの設定(Configuration)  
  * General  
  Tether1-4 Directionは線の1～4番目についた人が行く方角  
  NoTether12Directionは、整列した上で線が付かなかった人が行く方角  
    * 新リリドの場合の設定例  
    
    | Position | Tether |  
    | ---- | ---- |  
    | West | Tether1Direction |  
    | East | Tether2Direction |  
    | West | Tether3Direction |  
    | East | Tether4Direction |  
    | West | NoTether12Direction |  
    | East | NoTether34Direction |  
      
  * 優先度設定(priority list)  
  優先度に従って設定する(上が西)  
      * 新リリドの優先度  
      北/西←H1H2MTSTD1D2D3D4→南/東  
  
| TH | TH |  
| ---- | ---- |  
| TD | TD |  
| TD | TD |  


## P2 Diamond Dust  

公式から、ダイヤモンドダストのスクリプト  
要設定  

```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P2%20Diamond%20Dust.cs
```

* スクリプトの設定(Configuration)  
  AoE設置と扇誘導の部分は、Scriptでは方角と大まかな位置まで表示される。  
  細かい前後の立ち位置はPresetで表示する。  
  * Circle  
  アクスキックの時の散開方角  
    * NoAoE  
    自分にAoEが付いていない場合の方角を入力  
    * HasAoE  
    自分にAoEが付いている場合の方角を入力  
  * Donut  
  サイスキックの時の散開方角  
    * NoAoE  
    自分にAoEが付いていない場合の方角を入力  
    * HasAoE  
    自分にAoEが付いている場合の方角を入力  
  * Knockback  
  自分がノックバック担当になる方角を全て入力（通常、4つチェックする。）  
  * BaitColor  
    お好みで  
    Color1,2 `#00FF00FF`  
  * Predict Bait  
    Show Predict Baitにチェック  
    お好みで  
    Color `#00FF00FF`  
* スクリプトの設定(Registered Element)  
  お好みで  
  下記をコピーして、Ctrlを押しながら`Import custmized setting from clipboard`を左クリック  
```
{"Elements":{"Bait":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":3.0,"color":4278255389,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":true,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"Predict":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":3.0,"color":4278255389,"Filled":false,"fillIntensity":0.39215687,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":true,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0}}}
```

## P2 Mirror Mirror  

公式から、鏡の国のプリセット  
要設定  
```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P2%20Mirror%20Mirror.cs
```

* スクリプトの設定(Configuration)  
  * General  
  扇誘導をするときの、担当の位置を選択する  
    *  First Action  
    最初の扇誘導の担当位置。  
    新リリドの場合、近接は`OppositeBlueMirror`、遠隔は`BlueMirror`に設定。  
    *  Clockwise  
    赤鏡への距離が等しい時の担当位置を設定  
    ※赤鏡への距離が等しくない場合、近い赤鏡に誘導される。  
    新リリドの場合、`Clockwise`に設定
* スクリプトの設定(Registered Element)  
  お好みで  
```
{"Elements":{"Bait":{"Name":"","type":0,"Enabled":true,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":87.27208,"offY":112.72792,"offZ":0.0,"radius":2.0,"color":3355639552,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":true,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0}}}
```

## P2 Light Rampant JP  

公式から、光の暴走のスクリプト  

```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P2%20Light%20Rampant%20JP.cs
```

* スクリプトの設定(configuration)  
  * Players Count  
  自分より優先度が高い人数に合わせて変更する。  
  ここで載せている優先度では、D1は0、D2は1、D3は2、D4は3  
  * 優先度と自分が入る塔の設定  
  公式でわかりやすく説明されている為、それをそのまま引用。  
  ※H2D4調整で下記の場合の設定例  


優先度  
```  
T1 T2 H1 H2
D1 D2 D3 D4
```  
設定例  
```
T1 - Put No name 
0 = NW // 1 = None // 2 = None

T2 - Put T1 name
0 = S // 1 = NW // 2 = None

H1 - T1 > T2
0 = NE // 1 = S // 2 =  NW

H2 -T1 > T2 > H1
0 = SW // 1 = NE // 2 = S
--------------------------
D1 - Put no name
0 = SE // 1 = None // 2 = None

D2 - Put D1 name
0 = N // 1 = SE // 2 = None

D3- D1 > D2
 0 = SW // 1 = N // 2 = SE

D4 -  D1 > D2 > D3
0 = NE// 1=  SW -// 2 = N
```


## Layout
