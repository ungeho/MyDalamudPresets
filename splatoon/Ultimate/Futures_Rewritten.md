# Futures Rewritten  


調整中  

最終更新日：----  

## 概要-Abstract  

※現在作成中です(WIP)

公式のScriptとLemegetonの一部の機能を併用して使用することをお勧めします。  
設定方法も載せています。攻略方法に合わせて読み替えながら設定して使用してください。  
[Script](#Script)  
[Lemegeton](#Lemegeton)  


Presetはこちらです。  
[WIP] 現在作成中です。  
[JP/EN]トリガーや敵の名前、キャストID等の設定は英語と日本語に対応しています。  
※ただし、表示されるメッセージは日本語である事に注意してください。  
[Preset](#Preset)  
現在作成中の為、`P4-1`までしか表示されません。  
また、野良主流の攻略法で攻略を行っていない為、初期設定は野良主流(リリードール)とは別のものになっています。完成した際には、野良主流に合わせた初期設定にしてアップする予定です。  



## Script  

### P1 Cyclonic Break  

公式から、サイクロニックブレイクのスクリプト  
設定不要  

```
https://raw.githubusercontent.com/PunishXIV/Splatoon/refs/heads/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P1%20Cyclonic%20Break.cs
```
<!-- 
### P1 Fall of Faith  

公式のシンソイルセヴァーのスクリプトの移動先の座標を、ボスのタゲサ上になるように変更したもの。  
  
2025-02-06 現在では、このスクリプトで表示されるのは線の属性と自分が東西どちらかであるかの情報のみ  
それ以上の調整や立ち位置をやりやすくする為の表示についてはPresetで補完する。  



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
  
 -->


### P2 Diamond Dust  

公式から、ダイヤモンドダストのスクリプト  

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

### P2 Mirror Mirror  

公式から、鏡の国のスクリプト  

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

### P2 Light Rampant JP  

公式から、光の暴走のスクリプト  

```
https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P2%20Light%20Rampant%20JP.cs
```

* スクリプトの設定(configuration)  
  * Players Count  
  自分より優先度が高い人の数に合わせて変更する。  
  ここで載せている優先度では、D1は0、D2は1、D3は2、D4は3  
  * 優先度と自分が入る塔の設定  
  公式でわかりやすく説明されている為、それをそのまま引用。  
  塔の方角を指定する枠の隣の数字は、自分より優先度が高い人達の中で、AoEが付与された人の数。  
  AoEが付与された人数なので、D1は0、D2は0～1、D3は0～2、D4は0～2を設定していく。不要な部分はNoneに設定。  
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

### P3 Ultimate Relativity

公式から、絶・時間圧縮のスクリプト  

```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P3%20Ultimate%20Relativity.cs
```


`A Realm Recorded`を使ってリプレイを閲覧しながら、`Debug`タブで全員の配置が自分が行いたい処理と合致しているかを確認する事をお勧めします。  
* スクリプトの設定(Configuration)  
  THのファイガ30秒(2名)とDPSのファイガ10秒(2名)の被りへの対処は、優先度処理とマーカー処理の二種類ある。  
  それに応じて、スクリプトの設定も二種類用意されている。  
  `General`->`Mode`の部分で、Priority(優先度処理)かMarker(マーカー処理)を設定する。  
  * Priority(優先度)  
    大の字になるようにした時に`北西,北,北東,東`と`南東,南,南西,西`のグループに分けたい場合は`Base Orientation is North`にチェックする。  
    `西,北西,北,北東`と`東,南東,南,南西`のグループに分けたい場合は`Base Orientation is North`のチェックを外す。  
    これは日本の処理方法と海外の処理方法の違いで、20秒ファイガ担当(THとDPSから1名ずつ選出される)が`東`と`西`（時計と紫の線が繋がっている場所）のどちらの担当になるかという違いです。  
    以下は、`Base Orientation is North`にチェックし、時計に繋がる線が大の字になるように画面を合わせた状態だと仮定した説明です。  
    大の字の南側4名`南東,南,南西,西`の、30秒ファイガの担当位置を優先度設定の上位4名に入力します。  
    `南西`をMT、`南東`をH2としたい場合は、上から`MTSTH1H2`のように入力します。  
    次に、大の字の北側4名`北西,北,北東,東`の、10秒ファイガの担当位置を優先度設定の下位4名に入力します。  
    `北西`をD1、`北東`をD4としたい場合は、下位4名の部分に上から`D1D2D3D4`のように入力します。  
  * Marker  
    多分リリドはこちらです。  
    各デバフに対するマーカー付与と各マーカーに基づいた移動方向が設定されます。  
    実際に試していない為、設定方法はわかりません。  
    マーカー依存の設定は、光のツーラー様のブログをご確認ください。  
  * Bait Color  
    お好みで`Color1`と`Color2`共に`#00FF00FF`  
* スクリプトの設定(Registered Element)  
  Presetで既に大量に描画している都合上、ビームが表示されていると視認性が悪くなる為消しています。  
  お好みで  
```
{"Elements":{"East":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":110.62518,"offY":97.15299,"offZ":0.0,"radius":0.5,"color":4278190335,"Filled":false,"fillIntensity":0.39215687,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"Text":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":100.0,"offY":100.0,"offZ":0.0,"radius":0.0,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":5.0,"overlayFScale":5.0,"overlayPlaceholders":false,"thicc":2.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"SouthEast":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":2.0,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"South":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":2.0,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"SouthWest":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":2.0,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"West":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":2.0,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"NorthWest":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":2.0,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"North":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":2.0,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"NorthEast":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":2.0,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"SinboundMeltdown0":{"Name":"","type":2,"Enabled":true,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.0,"color":3355443455,"Filled":false,"fillIntensity":0.109,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":0.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"SinboundMeltdown1":{"Name":"","type":2,"Enabled":true,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.0,"color":3355443455,"Filled":false,"fillIntensity":0.124,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":0.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"SinboundMeltdown2":{"Name":"","type":2,"Enabled":true,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.0,"color":3355443455,"Filled":false,"fillIntensity":0.109,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":0.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0}}}
```
* その他補足  
  時計のビーム誘導位置は、Presetで表示されるもの（近接が殴れる位置）に従う方が、より真心模様に対して正確であり、幸せな気がします。  


### P3 Apocalypse  

公式から、アポカリプスのスクリプト  

```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P3%20Apocalypse.cs
```
* スクリプトの設定(Configuration)  
  `Show Initial movement` にチェック  
  `Show move guide for party(arrows to safe from initial movement)` にチェック  
  `Show tank bait guide(beta)`にチェック  
<!--
  * Safe spots and adjustments  
    * Select 4 safe spot potisions for your default group  
      しのしょーの場合、TH組は最初のAoEに対して赤or黄のマーカーとその+/-45度のマーカー（時計or反時計の一つ先のマーカー）なので  
      赤色のマーカーと黄色のマーカーのある位置、つまり`A1B2`(北,北東,東,南東)の位置にチェックを入れます。  
      DPSの場合は水色と紫色のマーカーなので、`C3D4`(南、南西、西、北西)の位置にチェックを入れます。  
      これは、リリドの場合でも同じ考え方で設定します。
    * Your default stack(when looking at Gaia):  
      3回目の頭割りで、デバフの入れ替わりが無い場合にガイアを見た時の左右どちらで頭割りを受けるか設定します。  
      しのしょーの場合、THは`Left`DPSは`Right`に設定します。  
      これはリリドの場合でも同じ考え方で設定します。
-->

`Safe spots and adjustments`の設定や、`優先度`の設定は行いません。  
安置への誘導の為に担当の方角にチェックを入れる項目がありますが、しのしょーやリリドの攻略法の場合では1回目のAoEに対して垂直になる方角とチェックした方角が一致した場所の安置に誘導されて欲しいのですが、実際には安置にチェックした方角が含まれていた場所に誘導されます。（多分北が優先度として一番高く、時計回りに優先度が下がっていく。）  
その為、担当の安置として誤った場所に誘導されてしまいます。現在(2025-02-20 14:55:16)のスクリプトの状態では`Safe spots and adjustments`以降の設定はしない方が良いと考えます。  
安置の場所やAoEの範囲の表示にこのスクリプトを使用して、自分の担当の散開位置は自分で判断するのが無難です。  
最小限の設定でも1回目のAoEの予兆が出る前、アポカリプス詠唱開始から安置の判断が出来る為、通常より多くの判断時間があります。  


* スクリプトの設定(Registered Elements)  
  描画されるものが多い為、AoEの早期表示を表示しないようにしています。  
  さらに、1回目のAoEに対して垂直になるマーカーの位置を確認しやすいように、1回目のAoEのラインと安置方向（AoE回転方向とは逆）へ示される矢印の描画を太くしています。  
  お好みで導入してください。  
```
{"Elements":{"EarlyCircle0":{"Name":"Circle","type":0,"Enabled":false,"refX":114.00072,"refY":100.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.0,"color":3355508223,"Filled":true,"fillIntensity":0.25,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":0.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"EarlyCircle1":{"Name":"Circle","type":0,"Enabled":false,"refX":85.99928,"refY":100.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.0,"color":3355508223,"Filled":true,"fillIntensity":0.25,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":0.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"EarlyCircle2":{"Name":"Circle","type":0,"Enabled":false,"refX":100.0,"refY":100.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.0,"color":3355508223,"Filled":true,"fillIntensity":0.25,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":0.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"EarlyCircle3":{"Name":"Circle","type":0,"Enabled":false,"refX":100.0,"refY":100.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.0,"color":3355508223,"Filled":true,"fillIntensity":0.25,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":0.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"EarlyCircle4":{"Name":"Circle","type":0,"Enabled":false,"refX":100.0,"refY":100.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.0,"color":3355508223,"Filled":true,"fillIntensity":0.25,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":0.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"EarlyCircle5":{"Name":"Circle","type":0,"Enabled":false,"refX":100.0,"refY":100.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.0,"color":3355508223,"Filled":true,"fillIntensity":0.25,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":0.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"Line2":{"Name":"","type":2,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.0,"color":3355443455,"Filled":true,"fillIntensity":0.345,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":16.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"LineRot1":{"Name":"","type":2,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.0,"color":3355508484,"Filled":true,"fillIntensity":0.345,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":16.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":1,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"LineRot2":{"Name":"","type":2,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.0,"color":3355508484,"Filled":true,"fillIntensity":0.345,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":16.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":1,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0}}}
```

### P4 Auto target switcher  

公式から、2体フェーズの均等化スクリプト  
設定不要  

```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P4%20AutoTargetSwitcher.cs
```


### P4 Darklit Dragonsong  

公式から、光と闇の竜詩のスクリプト  

```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P4%20Darklit.cs
```

* スクリプトの設定(Configuration)  
  * Mode  
    * Vertical(垂直)  
      東西にTHとDPSが分かれて、北を見た時に縦に整列している場合はこちらを設定します。  
      しのしょー(イディル)の処理方法はこちらです。  
    * Horizonal(水平)  
      南北にTHとDPSが分かれて、北を見た時に横に整列している場合はこちらを設定します。  
      リリド(ぬけまる)の処理方法はこちらです。  
  * Priority(優先度設定)  
    * Verticalに設定した場合  
      線が付いた人達の中で、上から`北西,南西,北東,南東`に配置される優先度を設定します。  
    * Horizonalに設定した場合  
      線が付いた人達の中で、上から`北西,北東,南西,南東`に配置される優先度を設定します。  
  * Box Swap Type(付与された線が四角形だった場合の処理方法)  
    処理方法に合わせて、交代する2名の方角を選択します。  
  * Hourglass Swap Type(付与された線が砂時計型だった場合の処理方法)  
    処理方法に合わせて、交代する2名の方角、または全員の回転方向を選択します。  
  * Tank Settings  
    必要に応じて、以下の項目をチェックする。  
    * Show Tank 1st Bait Guide  
      1回目(遠い人)誘導のタンク強  
    * Show Tank 2nd Bait Guide  
      2回目(近い人)誘導のタンク強  
* スクリプトの設定(Registered Elements)  
  念の為、誘導表示される円のFill(塗りつぶし)のチェックを全て外しています。  

```
{"Elements":{"North":{"Name":"","type":0,"Enabled":true,"refX":100.0,"refY":92.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":4.0,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"NorthEast":{"Name":"","type":0,"Enabled":true,"refX":103.0,"refY":99.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.5,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"SouthEast":{"Name":"","type":0,"Enabled":true,"refX":103.0,"refY":101.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.5,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"South":{"Name":"","type":0,"Enabled":true,"refX":100.0,"refY":108.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":4.0,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"SouthWest":{"Name":"","type":0,"Enabled":true,"refX":97.0,"refY":101.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.5,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"NorthWest":{"Name":"","type":0,"Enabled":true,"refX":97.0,"refY":99.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.5,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"Split":{"Name":"","type":0,"Enabled":true,"refX":100.0,"refY":100.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.0,"color":4294901760,"Filled":false,"fillIntensity":0.39215687,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":3.0,"overlayFScale":4.0,"overlayPlaceholders":false,"thicc":2.0,"overlayText":"<< SPLIT >>","refActorTargetingYou":0,"refActorPlaceholder":[],"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":5,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"Stack":{"Name":"","type":0,"Enabled":true,"refX":100.0,"refY":100.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.0,"color":4294901760,"Filled":false,"fillIntensity":0.39215687,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":3.0,"overlayFScale":4.0,"overlayPlaceholders":false,"thicc":2.0,"overlayText":"<< STACK >>","refActorTargetingYou":0,"refActorPlaceholder":[],"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":5,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"StackBaitNorth":{"Name":"","type":0,"Enabled":true,"refX":0.0,"refY":87.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":1.0,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"StackBaitSouth":{"Name":"","type":0,"Enabled":true,"refX":0.0,"refY":113.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":1.0,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"TankBait":{"Name":"","type":0,"Enabled":true,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":1.0,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0}}}
```

### P4 Crystallize Time  

公式の時間結晶のスクリプトから、青デバフの解除の誘導を取り除いたもの。  
自分で自分にマーカーを付与する場合のマーカー処理、あるいはデバフ処理で行う場合の時間結晶のスクリプトは問題ないと思います。  
しかし、マーカー処理かつ他人にマーカーを付与してもらう場合や死人が出た場合の青デバフ解除の誘導表示にやや問題がありました。  
その為、青デバフ解除の誘導機能を取り除いています。  
また、代替表示をプリセットに入れています。  

```
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.Automation;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using ECommons.MathHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Splatoon;
using Splatoon.SplatoonScripting;
using Splatoon.SplatoonScripting.Priority;
using Splatoon.Utility;
using Action = Lumina.Excel.Sheets.Action;

namespace SplatoonScriptsOfficial.Duties.Dawntrail.The_Futures_Rewritten;

public unsafe class P4_Crystallize_Time : SplatoonScript
{
    public enum Direction
    {
        North = 0,
        NorthEast = 45,
        East = 90,
        SouthEast = 135,
        South = 180,
        SouthWest = 225,
        West = 270,
        NorthWest = 315
    }

    private readonly Vector2 _center = new(100, 100);

    private readonly List<IBattleChara> _earlyHourglassList = [];
    private readonly List<IBattleChara> _lateHourglassList = [];

    private readonly Dictionary<ulong, PlayerData> _players = new();

    private readonly IEnumerable<uint> AllDebuffIds = Enum.GetValues<Debuff>().Cast<uint>();

    private Direction? _baseDirection = Direction.North;
    private string _basePlayerOverride = "";

    private Direction _debugDirection1 = Direction.North;
    private Direction _debugDirection2 = Direction.North;

    private Direction _editSplitElementDirection;
    private float _editSplitElementRadius;

    private Direction? _firstWaveDirection;

    private Direction? _lateHourglassDirection;
    private Direction? _secondWaveDirection;

    private List<float> ExtraRandomness = [];
    private bool Initialized;
    public override Metadata? Metadata => new(12, "Garume, NightmareXIV");

    public override Dictionary<int, string> Changelog => new()
    {
        [10] =
            "A large addition of various functions as well as changes to general mechanic flow. Please validate settings and if possible verify that the script works fine in replay.",
        [11] = "Added dragon explosion anticipation for eruption"
    };

    private IPlayerCharacter BasePlayer
    {
        get
        {
            if (_basePlayerOverride == "")
                return Player.Object;
            return Svc.Objects.OfType<IPlayerCharacter>()
                .FirstOrDefault(x => x.Name.ToString().EqualsIgnoreCase(_basePlayerOverride)) ?? Player.Object;
        }
    }

    private float SpellInWaitingDebuffTime =>
        BasePlayer.StatusList?.FirstOrDefault(x => x.StatusId == (uint)Debuff.DelayReturn)?.RemainingTime ?? -1f;

    private float ReturnDebuffTime =>
        BasePlayer.StatusList?.FirstOrDefault(x => x.StatusId == (uint)Debuff.Return)?.RemainingTime ?? -1f;

    private bool IsActive => Svc.Objects.Any(x => x.DataId == 17837) && !BasePlayer.IsDead;

    public override HashSet<uint>? ValidTerritories => [1238];

    private Config C => Controller.GetConfig<Config>();

    private static IBattleNpc? WestDragon => Svc.Objects.Where(x => x is { DataId: 0x45AC, Position.X: <= 100 })
        .Select(x => x as IBattleNpc).First();

    private static IBattleNpc? EastDragon => Svc.Objects.Where(x => x is { DataId: 0x45AC, Position.X: > 100 })
        .Select(x => x as IBattleNpc).First();

    private static IEnumerable<IEventObj> Cleanses => Svc.Objects.Where(x => x is { DataId: 0x1EBD41 })
        .OfType<IEventObj>()
        .OrderBy(x => x.Position.X);

    private MechanicStage GetStage()
    {
        if (Svc.Objects.All(x => x.DataId != 17837)) return MechanicStage.Unknown;
        var time = SpellInWaitingDebuffTime;
        if (time > 0)
            return time switch
            {
                < 11.5f => MechanicStage.Step6_ThirdHourglass,
                < 15.6f => MechanicStage.Step5_PerformDodges,
                < 16.5f => MechanicStage.Step4_SecondHourglass,
                < 18.8f => MechanicStage.Step3_IcesAndWinds,
                < 21.9f => MechanicStage.Step2_FirstHourglass,
                _ => MechanicStage.Step1_Spread
            };
        var returnTime = ReturnDebuffTime;
        return returnTime > 0 ? MechanicStage.Step7_SpiritTaker : MechanicStage.Unknown;
    }


    public override void OnStartingCast(uint source, uint castId)
    {
        if (GetStage() == MechanicStage.Unknown) return;
        if (castId == 40251 && source.GetObject() is { } sourceObject)
        {
            var direction = GetDirection(sourceObject.Position);
            if (direction == null) return;
            if (_firstWaveDirection == null)
                _firstWaveDirection = direction;
            else
                _secondWaveDirection = direction;
        }
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        if (GetStage() == MechanicStage.Unknown) return;
        if (vfxPath == "vfx/common/eff/dk02ht_zan0m.avfx" &&
            target.GetObject() is IBattleNpc piece &&
            _baseDirection == null)
        {
            var newDirection = GetDirection(piece.Position);
            if (newDirection != null) _baseDirection = newDirection;
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (GetStage() == MechanicStage.Unknown) return;
        if (source.GetObject() is not IBattleChara sourceObject) return;
        if (data5 == 15)
        {
            switch (data3)
            {
                case 133:
                    _lateHourglassList.Add(sourceObject);
                    break;
                case 134:
                    _earlyHourglassList.Add(sourceObject);
                    break;
            }

            if (_lateHourglassList.Count == 2 && _earlyHourglassList.Count == 2)
            {
                var newDirection = GetDirection(_lateHourglassList[0].Position);
                if (newDirection != null) _lateHourglassDirection = newDirection;
            }
        }
    }

    private static Direction? GetDirection(Vector3? positionNullable)
    {
        if (positionNullable == null) return null;
        var position = positionNullable.Value;
        var isNorth = position.Z < 95f;
        var isEast = position.X > 105f;
        var isSouth = position.Z > 105f;
        var isWest = position.X < 95f;

        if (isNorth && isEast) return Direction.NorthEast;
        if (isEast && isSouth) return Direction.SouthEast;
        if (isSouth && isWest) return Direction.SouthWest;
        if (isWest && isNorth) return Direction.NorthWest;
        if (isNorth) return Direction.North;
        if (isEast) return Direction.East;
        if (isSouth) return Direction.South;
        if (isWest) return Direction.West;
        return null;
    }

    public override void OnGainBuffEffect(uint sourceId, Status Status)
    {
        if (!IsActive || Initialized || sourceId.GetObject() is not IPlayerCharacter player) return;
        var debuffs = player.StatusList.Where(x => AllDebuffIds.Contains(x.StatusId));

        _players.TryAdd(player.GameObjectId, new PlayerData { PlayerName = player.Name.ToString() });

        foreach (var debuff in debuffs)
            switch (debuff.StatusId)
            {
                case (uint)Debuff.Red:
                    _players[player.GameObjectId].Color = Debuff.Red;
                    break;
                case (uint)Debuff.Blue:
                    _players[player.GameObjectId].Color = Debuff.Blue;
                    break;
                case (uint)Debuff.Quietus:
                    _players[player.GameObjectId].HasQuietus = true;
                    break;
                case (uint)Debuff.DelayReturn:
                    break;
                default:
                    _players[player.GameObjectId].Debuff = (Debuff)debuff.StatusId;
                    break;
            }


        if (_players.All(x => x.Value.HasDebuff))
        {
            var redBlizzards = C.PriorityData
                .GetPlayers(x => _players.First(y => y.Value.PlayerName == x.Name).Value is
                    { Color: Debuff.Red, Debuff: Debuff.Blizzard }
                );

            if (redBlizzards != null)
            {
                _players[redBlizzards[0].IGameObject.GameObjectId].MoveType = MoveType.RedBlizzardWest;
                _players[redBlizzards[1].IGameObject.GameObjectId].MoveType = MoveType.RedBlizzardEast;
            }

            var redAeros = C.PriorityData
                .GetPlayers(x => _players.First(y => y.Value.PlayerName == x.Name).Value is
                    { Color: Debuff.Red, Debuff: Debuff.Aero }
                );

            if (redAeros != null)
            {
                _players[redAeros[0].IGameObject.GameObjectId].MoveType = MoveType.RedAeroWest;
                _players[redAeros[1].IGameObject.GameObjectId].MoveType = MoveType.RedAeroEast;
            }

            foreach (var otherPlayer in _players.Where(x => x.Value.MoveType == null))
                _players[otherPlayer.Key].MoveType = otherPlayer.Value.Debuff switch
                {
                    Debuff.Holy => MoveType.BlueHoly,
                    Debuff.Blizzard => MoveType.BlueBlizzard,
                    Debuff.Water => MoveType.BlueWater,
                    Debuff.Eruption => MoveType.BlueEruption,
                    _ => _players[otherPlayer.Key].MoveType
                };


            if (!string.IsNullOrEmpty(C.CommandWhenBlueDebuff) &&
                BasePlayer.StatusList.Any(x => x.StatusId == (uint)Debuff.Blue))
            {
                var random = 0;
                if (C.ShouldUseRandomWait)
                    random = RandomNumberGenerator.GetInt32((int)(C.WaitRange.X * 1000), (int)(C.WaitRange.Y * 1000));
                Controller.Schedule(() => { Chat.Instance.ExecuteCommand(C.CommandWhenBlueDebuff); }, random);
            }

            Initialized = true;
            PluginLog.Debug("CT initialized");
        }
    }

    public override void OnReset()
    {
        Initialized = false;
        _baseDirection = null;
        _lateHourglassDirection = null;
        _firstWaveDirection = null;
        _secondWaveDirection = null;
        _players.Clear();
        _earlyHourglassList.Clear();
        _lateHourglassList.Clear();
        ExtraRandomness =
        [
            (float)Random.Shared.NextDouble() - 0.5f, (float)Random.Shared.NextDouble() - 0.5f,
            (float)Random.Shared.NextDouble() - 0.5f, (float)Random.Shared.NextDouble() - 0.5f
        ];
    }


    private Vector2 SwapXIfNecessary(Vector2 position)
    {
        if (_lateHourglassDirection is Direction.NorthEast or Direction.SouthWest)
            return position;
        var swapX = _center.X * 2 - position.X;
        return new Vector2(swapX, position.Y);
    }

    public override void OnSetup()
    {
        foreach (var move in Enum.GetValues<MoveType>())
            Controller.RegisterElement(move.ToString(), new Element(0)
            {
                radius = 1f,
                thicc = 6f
            });

        foreach (var stack in Enum.GetValues<WaveStack>())
            Controller.RegisterElement(stack + nameof(WaveStack), new Element(0)
            {
                radius = 0.5f,
                thicc = 6f
            });

        Controller.RegisterElement("Alert", new Element(1)
        {
            radius = 0f,
            overlayText = "Alert",
            overlayFScale = 1f,
            overlayVOffset = 1f,
            refActorComparisonType = 5,
            refActorPlaceholder = ["<1>"]
        });

        Controller.RegisterElementFromCode("SplitPosition",
            "{\"Name\":\"\",\"Enabled\":false,\"refX\":100.0,\"refY\":100.0,\"radius\":1.0,\"Filled\":false,\"fillIntensity\":0.5,\"overlayBGColor\":4278190080,\"overlayTextColor\":4294967295,\"thicc\":4.0,\"overlayText\":\"Spread!\",\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        Controller.RegisterElementFromCode("KBHelper",
            "{\"Name\":\"\",\"type\":2,\"Enabled\":false,\"radius\":0.0,\"color\":3355508503,\"fillIntensity\":0.345,\"thicc\":4.0,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        Controller.RegisterElementFromCode("RedDragonExplosion1",
            "{\"Name\":\"\",\"refX\":87.5,\"refY\":98.0,\"refZ\":1.9073486E-06,\"radius\":13.0,\"color\":3372155112,\"fillIntensity\":0.5,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("RedDragonExplosion2",
            "{\"Name\":\"\",\"refX\":112.5,\"refY\":98.0,\"refZ\":1.9073486E-06,\"radius\":13.0,\"color\":3372155112,\"fillIntensity\":0.5,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
    }

    private void Alert(string text)
    {
        var playerOrder = GetPlayerOrder(BasePlayer);
        if (Controller.TryGetElementByName("Alert", out var element))
        {
            element.Enabled = true;
            element.overlayText = text;
            element.refActorPlaceholder = [$"<{playerOrder}>"];
        }
    }

    private static int GetPlayerOrder(IGameObject c)
    {
        for (var i = 1; i <= 8; i++)
            if ((nint)FakePronoun.Resolve($"<{i}>") == c.Address)
                return i;

        return 0;
    }

    private void HideAlert()
    {
        if (Controller.TryGetElementByName("Alert", out var element))
            element.Enabled = false;
    }


    public override void OnUpdate()
    {
        ProcessAutoCast();

        if (GetStage() == MechanicStage.Unknown)
        {
            Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
            return;
        }

        var spr = GetStage().EqualsAny(MechanicStage.Step1_Spread, MechanicStage.Step2_FirstHourglass) &&
                  BasePlayer.StatusList.Any(x => x.StatusId == (uint)Debuff.Eruption) &&
                  SpellInWaitingDebuffTime < 25f && Svc.Objects.OfType<IPlayerCharacter>().Any(x =>
                      x.StatusList.Count(s => s.StatusId.EqualsAny((uint)Debuff.Red, (uint)Debuff.Blizzard)) == 2);
        Controller.GetElementByName("RedDragonExplosion1")!.Enabled = spr;
        Controller.GetElementByName("RedDragonExplosion2")!.Enabled = spr;


        {
            var e = Controller.GetElementByName("KBHelper")!;
            e.Enabled = false;
            if (GetStage() == MechanicStage.Step2_FirstHourglass &&
                BasePlayer.StatusList.Any(x => x.StatusId == (uint)Debuff.Blue))
            {
                var wind = Svc.Objects.OfType<IPlayerCharacter>()
                    .OrderBy(x => Vector3.Distance(x.Position, BasePlayer.Position))
                    .Where(x => x.StatusList.Any(s => s.StatusId == (uint)Debuff.Aero)).FirstOrDefault();
                if (wind != null && Vector3.Distance(BasePlayer.Position, wind.Position) < 5f)
                {
                    e.Enabled = true;
                    e.SetRefPosition(wind.Position);
                    e.SetOffPosition(new Vector3(
                        100 + (_lateHourglassDirection.EqualsAny(Direction.NorthEast, Direction.SouthWest) ? 12 : -12),
                        0, 85));
                }
            }
        }

        var myMove = _players.SafeSelect(BasePlayer.GameObjectId)?.MoveType.ToString();
        var forcedPosition = ResolveRedAeroMove();
        forcedPosition ??= ResolveRedBlizzardMove();
        if (myMove != null)
            foreach (var move in Enum.GetValues<MoveType>())
                if (Controller.TryGetElementByName(move.ToString(), out var element))
                {
                    if (GetStage() == MechanicStage.Step6_ThirdHourglass &&
                        BasePlayer.StatusList.All(x => x.StatusId != (uint)Debuff.Blue))
                    {
                        element.Enabled = false;
                        continue;
                    }

                    element.Enabled = C.ShowOther;
                    element.color = EColor.Red.ToUint();

                    if (myMove == move.ToString())
                    {
                        element.Enabled = true;
                        element.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
                        element.tether = true;
                        if (forcedPosition == null) continue;
                        element.SetOffPosition(forcedPosition.Value.ToVector3(0));
                        element.radius = 0.4f;
                    }
                }


        if (forcedPosition != null) return;
        switch (GetStage())
        {
            case MechanicStage.Step1_Spread:
                BurnHourglassUniversal();
                break;
            case MechanicStage.Step2_FirstHourglass:
                IceHitDragon();
                break;
            case MechanicStage.Step3_IcesAndWinds:
                BurnHourglassUniversal();
                break;
            case MechanicStage.Step4_SecondHourglass:
                if (C.HitTiming == HitTiming.Early && BasePlayer.StatusList.Any(x => x.StatusId == (uint)Debuff.Red))
                    HitDragonAndAero();
                else
                    BurnHourglassUniversal();
                break;
            case MechanicStage.Step5_PerformDodges:
                if (C.HitTiming == HitTiming.Late && BasePlayer.StatusList.Any(x => x.StatusId == (uint)Debuff.Red))
                    HitDragonAndAero();
                else
                    BurnHourglassUniversal();
                break;
            case MechanicStage.Step6_ThirdHourglass:
                // if (BasePlayer.StatusList.Any(x => x.StatusId == (uint)Debuff.Blue))
                //     CorrectCleanse();
                // else
                //     PlaceReturn();
                PlaceReturn();
                break;
            case MechanicStage.Step7_SpiritTaker:
                Split();
                break;
        }
    }

    private void BurnHourglassUniversal()
    {
        if (GetStage() < MechanicStage.Step2_FirstHourglass) BurnYellowHourglass();
        else if (GetStage() < MechanicStage.Step4_SecondHourglass) BurnHourglass();
        else if (GetStage() < MechanicStage.Step6_ThirdHourglass) BurnPurpleHourglass();
    }

    private void AutoCast(uint actionId)
    {
        if (!Svc.Condition[ConditionFlag.DutyRecorderPlayback])
        {
            if (ActionManager.Instance()->GetActionStatus(ActionType.Action, actionId) == 0 &&
                EzThrottler.Throttle(InternalData.FullName + "AutoCast", 100))
                Chat.Instance.ExecuteAction(actionId);
        }
        else
        {
            if (EzThrottler.Throttle(InternalData.FullName + "InformCast", 100))
                DuoLog.Information(
                    $"Would use mitigation action {ExcelActionHelper.GetActionName(actionId)} if possible");
        }
    }

    private void ProcessAutoCast()
    {
        try
        {
            if (Svc.Objects.Any(x => x.DataId == 17837) && !BasePlayer.IsDead)
            {
                if (C.UseKbiAuto &&
                    BasePlayer.StatusList.Any(x =>
                        x.StatusId == (uint)Debuff.Return && x.RemainingTime < 2f + ExtraRandomness.SafeSelect(0)))
                    //7559 : surecast
                    //7548 : arm's length
                    UseAntiKb();

                if (C.UseMitigation && C.MitigationAction != 0 &&
                    BasePlayer.StatusList.Any(x =>
                        x.StatusId == (uint)Debuff.Return && x.RemainingTime < 6f + ExtraRandomness.SafeSelect(1)))
                    AutoCast(C.MitigationAction);

                if (C.UseTankMitigation && C.TankMitigationAction != 0 &&
                    BasePlayer.StatusList.Any(x =>
                        x.StatusId == (uint)Debuff.Return && x.RemainingTime < 6f + ExtraRandomness.SafeSelect(1)))
                    AutoCast(C.TankMitigationAction);

                if (C is { UseSprintAuto: true, ShouldGoNorthRedBlizzard: true } &&
                    BasePlayer.StatusList.Any(x => x.StatusId == (uint)Debuff.Red) &&
                    BasePlayer.StatusList.Any(x =>
                        x.StatusId == (uint)Debuff.Blizzard && x.RemainingTime < 1f + ExtraRandomness.SafeSelect(3)))
                    AutoCast(29057);
            }
        }
        catch (Exception e)
        {
            e.Log();
        }
    }

    private void UseAntiKb()
    {
        foreach (var x in (uint[]) [7559, 7548])
            if (!Svc.Condition[ConditionFlag.DutyRecorderPlayback])
            {
                if (ActionManager.Instance()->GetActionStatus(ActionType.Action, x) == 0 &&
                    EzThrottler.Throttle(InternalData.FullName + "AutoCast", 100)) Chat.Instance.ExecuteAction(x);
            }
            else
            {
                if (EzThrottler.Throttle(InternalData.FullName + "InformCast", 100))
                    DuoLog.Information(
                        $"Would use kb immunity action {ExcelActionHelper.GetActionName(x)} if possible");
            }
    }

    private void BurnYellowHourglass()
    {
        foreach (var player in Enum.GetValues<MoveType>())
        {
            var position = player switch
            {
                MoveType.RedBlizzardWest => new Vector2(87, 100),
                MoveType.RedBlizzardEast => new Vector2(113, 100),
                MoveType.RedAeroWest => new Vector2(88, 115),
                MoveType.RedAeroEast => new Vector2(112, 115),
                MoveType.BlueBlizzard => new Vector2(88, 115),
                MoveType.BlueHoly => new Vector2(88, 115),
                MoveType.BlueWater => new Vector2(88, 115),
                MoveType.BlueEruption => new Vector2(112, 85),
                _ => throw new InvalidOperationException()
            };

            position = SwapXIfNecessary(position);
            if (Controller.TryGetElementByName(SwapIfNecessary(player), out var element))
            {
                element.radius = 0.5f;
                element.SetOffPosition(position.ToVector3(0));
            }
        }
    }

    private void IceHitDragon()
    {
        foreach (var player in Enum.GetValues<MoveType>())
        {
            var position = player switch
            {
                MoveType.RedBlizzardWest => WestDragon?.Position.ToVector2() ?? new Vector2(87, 100),
                MoveType.RedBlizzardEast => EastDragon?.Position.ToVector2() ?? new Vector2(113, 100),
                MoveType.RedAeroWest => new Vector2(90, 117),
                MoveType.RedAeroEast => new Vector2(107, 118),
                MoveType.BlueBlizzard => new Vector2(91, 115),
                MoveType.BlueHoly => new Vector2(91, 115),
                MoveType.BlueWater => new Vector2(91, 115),
                MoveType.BlueEruption => new Vector2(112, 85),
                _ => throw new InvalidOperationException()
            };

            position = SwapXIfNecessary(position);
            if (Controller.TryGetElementByName(SwapIfNecessary(player), out var element))
            {
                element.radius = 0.5f;
                element.SetOffPosition(position.ToVector3(0));
            }
        }

        var myMove = _players.SafeSelect(BasePlayer.GameObjectId)?.MoveType;
        if (myMove is MoveType.RedBlizzardEast or MoveType.RedBlizzardWest)
        {
            var remainingTime = BasePlayer.StatusList.FirstOrDefault(x => x.StatusId == (uint)Debuff.Blizzard)
                ?.RemainingTime;
            Alert(C.HitDragonText.Get() + (remainingTime != null ? $" ({remainingTime.Value:0.0}s)" : ""));
        }
    }

    private void BurnHourglass()
    {
        foreach (var player in Enum.GetValues<MoveType>())
        {
            var position = player switch
            {
                MoveType.RedBlizzardWest => new Vector2(112, 86),
                MoveType.RedBlizzardEast => new Vector2(112, 86),
                MoveType.RedAeroWest => new Vector2(100, 115),
                MoveType.RedAeroEast => new Vector2(107, 118),
                MoveType.BlueBlizzard => new Vector2(112, 86),
                MoveType.BlueHoly => new Vector2(112, 86),
                MoveType.BlueWater => new Vector2(112, 86),
                MoveType.BlueEruption => new Vector2(112, 86),
                _ => throw new InvalidOperationException()
            };

            position = SwapXIfNecessary(position);
            if (Controller.TryGetElementByName(SwapIfNecessary(player), out var element))
            {
                element.radius = 1f;
                element.SetOffPosition(position.ToVector3(0));
            }
        }
    }

    private void BurnPurpleHourglass()
    {
        foreach (var player in Enum.GetValues<MoveType>())
        {
            var position = player switch
            {
                MoveType.RedBlizzardWest => new Vector2(100, 85),
                MoveType.RedBlizzardEast => new Vector2(100, 85),
                MoveType.RedAeroWest => new Vector2(100, 118),
                MoveType.RedAeroEast => new Vector2(110, 110),
                MoveType.BlueBlizzard => new Vector2(100, 85),
                MoveType.BlueHoly => new Vector2(100, 85),
                MoveType.BlueWater => new Vector2(100, 85),
                MoveType.BlueEruption => new Vector2(100, 85),
                _ => throw new InvalidOperationException()
            };

            position = SwapXIfNecessary(position);
            if (Controller.TryGetElementByName(SwapIfNecessary(player), out var element))
            {
                element.radius = 1f;
                element.SetOffPosition(position.ToVector3(0));
            }
        }

        Alert(C.AvoidWaveText.Get());
    }

    private void HitDragonAndAero()
    {
        foreach (var player in Enum.GetValues<MoveType>())
        {
            Direction? returnDirection = (_firstWaveDirection, _secondWaveDirection) switch
            {
                (Direction.North, Direction.East) => Direction.NorthEast,
                (Direction.East, Direction.South) => Direction.SouthEast,
                (Direction.South, Direction.West) => Direction.SouthWest,
                (Direction.West, Direction.North) => Direction.NorthWest,
                (Direction.North, Direction.West) => Direction.NorthWest,
                (Direction.West, Direction.South) => Direction.SouthWest,
                (Direction.South, Direction.East) => Direction.SouthEast,
                (Direction.East, Direction.North) => Direction.NorthEast,
                _ => null
            };

            var returnPosition = returnDirection switch
            {
                Direction.NorthEast => new Vector2(115, 85),
                Direction.SouthEast => new Vector2(115, 115),
                Direction.SouthWest => new Vector2(85, 115),
                Direction.NorthWest => new Vector2(85, 85),
                _ => new Vector2(100f, 85f)
            };

            Vector2? position = player switch
            {
                MoveType.RedBlizzardWest => returnPosition,
                MoveType.RedBlizzardEast => returnPosition,
                MoveType.RedAeroWest => WestDragon?.Position.ToVector2() ?? new Vector2(87, 108),
                MoveType.RedAeroEast => EastDragon?.Position.ToVector2() ?? new Vector2(113, 108),
                //MoveType.BlueBlizzard => new Vector2(100, 100),
                //MoveType.BlueHoly => new Vector2(100, 100),
                //MoveType.BlueWater => new Vector2(100, 100),
                //MoveType.BlueEruption => new Vector2(100, 100),
                _ => null
            };

            if (position != null)
            {
                position = SwapXIfNecessary(position.Value);
                if (Controller.TryGetElementByName(SwapIfNecessary(player), out var element))
                {
                    element.radius = 2f;
                    element.SetOffPosition(position.Value.ToVector3(0));
                }
            }
        }

        var myMove = _players.SafeSelect(BasePlayer.GameObjectId)?.MoveType;
        if (myMove is MoveType.RedAeroEast or MoveType.RedAeroWest)
            Alert(C.HitDragonText.Get());
    }

    private string SwapIfNecessary(MoveType move)
    {
        if (_lateHourglassDirection is Direction.NorthEast or Direction.SouthWest)
            return move.ToString();
        return move switch
        {
            MoveType.RedBlizzardWest => MoveType.RedBlizzardEast.ToString(),
            MoveType.RedBlizzardEast => MoveType.RedBlizzardWest.ToString(),
            MoveType.RedAeroWest => MoveType.RedAeroEast.ToString(),
            MoveType.RedAeroEast => MoveType.RedAeroWest.ToString(),
            _ => move.ToString()
        };
    }

    private void CorrectCleanse()
    {
        foreach (var player in Enum.GetValues<MoveType>())
        {
            var direction = Direction.West;
            if (C.PrioritizeMarker &&
                _players.FirstOrDefault(x => x.Value.PlayerName == BasePlayer.Name.ToString()).Value?.Marker is
                    { } marker)
            {
                direction = marker switch
                {
                    MarkerType.Attack1 => C.WhenAttack1,
                    MarkerType.Attack2 => C.WhenAttack2,
                    MarkerType.Attack3 => C.WhenAttack3,
                    MarkerType.Attack4 => C.WhenAttack4,
                    _ => direction
                };
            }
            else
            {
                if (player == C.WestSentence)
                    direction = Direction.West;
                else if (player == C.SouthWestSentence)
                    direction = Direction.SouthWest;
                else if (player == C.SouthEastSentence)
                    direction = Direction.SouthEast;
                else if (player == C.EastSentence)
                    direction = Direction.East;
            }

            var cleanses = Cleanses.ToArray();

            var position = direction switch
            {
                Direction.West => cleanses[0].Position.ToVector2(),
                Direction.SouthWest => cleanses[1].Position.ToVector2(),
                Direction.SouthEast => cleanses[2].Position.ToVector2(),
                Direction.East => cleanses[3].Position.ToVector2(),
                _ => new Vector2(100, 100)
            };

            if (Controller.TryGetElementByName(SwapIfNecessary(player), out var element))
            {
                element.radius = 2f;
                element.SetOffPosition(position.ToVector3(0));
            }
        }

        if (BasePlayer.StatusList.Any(x => x.StatusId == (uint)Debuff.Blue))
            Alert(C.CleanseText.Get());
        else
            HideAlert();
    }

    private void PlaceReturn()
    {
        if (C.NukemaruRewind)
            NukemaruPlaceReturn();
        else if (C.KBIRewind)
            KBIPlaceReturn();
        else
            DefaultPlaceReturn();

        Alert(C.PlaceReturnText.Get());
    }

    private void KBIPlaceReturn()
    {
        var returnDirection = (_firstWaveDirection, _secondWaveDirection) switch
        {
            (Direction.North, Direction.East) => Direction.North,
            (Direction.East, Direction.South) => Direction.South,
            (Direction.South, Direction.West) => Direction.South,
            (Direction.West, Direction.North) => Direction.North,
            (Direction.North, Direction.West) => Direction.North,
            (Direction.West, Direction.South) => Direction.South,
            (Direction.South, Direction.East) => Direction.South,
            (Direction.East, Direction.North) => Direction.North,
            _ => throw new InvalidOperationException()
        };
        if (Controller.TryGetElementByName(WaveStack.West + nameof(WaveStack), out var myElement))
        {
            myElement.Enabled = true;
            myElement.tether = true;
            myElement.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
            myElement.SetOffPosition(Vector3.Zero);
            myElement.SetRefPosition(new Vector3(100, 0, 100 + (returnDirection == Direction.North ? -2 : 2)));
        }
    }

    private void NukemaruPlaceReturn()
    {
        var returnDirection = (_firstWaveDirection, _secondWaveDirection) switch
        {
            (Direction.North, Direction.East) => Direction.NorthEast,
            (Direction.East, Direction.South) => Direction.SouthEast,
            (Direction.South, Direction.West) => Direction.SouthWest,
            (Direction.West, Direction.North) => Direction.NorthWest,
            (Direction.North, Direction.West) => Direction.NorthWest,
            (Direction.West, Direction.South) => Direction.SouthWest,
            (Direction.South, Direction.East) => Direction.SouthEast,
            (Direction.East, Direction.North) => Direction.NorthEast,
            _ => throw new InvalidOperationException()
        };

        var basePosition = returnDirection switch
        {
            Direction.NorthEast => new Vector3(100, 0, 95),
            Direction.SouthEast => new Vector3(100, 0, 105),
            Direction.SouthWest => new Vector3(100, 0, 105),
            Direction.NorthWest => new Vector3(100, 0, 95),
            _ => throw new InvalidOperationException()
        };

        var direction = returnDirection switch
        {
            Direction.NorthEast => C.NukemaruRewindPositionWhenNorthEastWave,
            Direction.SouthEast => C.NukemaruRewindPositionWhenSouthEastWave,
            Direction.SouthWest => C.NukemaruRewindPositionWhenSouthWestWave,
            Direction.NorthWest => C.NukemaruRewindPositionWhenNorthWestWave,
            _ => throw new InvalidOperationException()
        };

        var position = basePosition +
                       MathHelper.RotateWorldPoint(Vector3.Zero, ((int)direction).DegreesToRadians(),
                           -Vector3.UnitZ * 3f);

        if (Controller.TryGetElementByName(WaveStack.West + nameof(WaveStack), out var myElement))
        {
            myElement.Enabled = true;
            myElement.tether = true;
            myElement.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
            myElement.SetOffPosition(Vector3.Zero);
            myElement.SetRefPosition(position);
        }
    }

    private void DefaultPlaceReturn()
    {
        var returnDirection = (_firstWaveDirection, _secondWaveDirection) switch
        {
            (Direction.North, Direction.East) => Direction.NorthEast,
            (Direction.East, Direction.South) => Direction.SouthEast,
            (Direction.South, Direction.West) => Direction.SouthWest,
            (Direction.West, Direction.North) => Direction.NorthWest,
            (Direction.North, Direction.West) => Direction.NorthWest,
            (Direction.West, Direction.South) => Direction.SouthWest,
            (Direction.South, Direction.East) => Direction.SouthEast,
            (Direction.East, Direction.North) => Direction.NorthEast,
            _ => throw new InvalidOperationException()
        };

        var basePosition = returnDirection switch
        {
            Direction.NorthEast => new Vector2(113, 87),
            Direction.SouthEast => new Vector2(113, 113),
            Direction.SouthWest => new Vector2(87, 113),
            Direction.NorthWest => new Vector2(87, 87),
            _ => throw new InvalidOperationException()
        };

        var isWest = returnDirection switch
        {
            Direction.NorthEast => C.IsWestWhenNorthEastWave,
            Direction.SouthEast => C.IsWestWhenSouthEastWave,
            Direction.SouthWest => C.IsWestWhenSouthWestWave,
            Direction.NorthWest => C.IsWestWhenNorthWestWave,
            _ => throw new InvalidOperationException()
        };

        var myStack = (isWest, C.IsTank) switch
        {
            (true, true) => WaveStack.WestTank,
            (false, true) => WaveStack.EastTank,
            (true, false) => WaveStack.West,
            (false, false) => WaveStack.East
        };

        var westTankPosition = basePosition;
        var eastTankPosition = basePosition;
        var westPosition = basePosition;
        var eastPosition = basePosition;

        switch (returnDirection)
        {
            case Direction.NorthEast:
                westTankPosition += new Vector2(-3f, -0.5f);
                eastTankPosition += new Vector2(0.5f, 3f);
                westPosition += new Vector2(-3f, 1f);
                eastPosition += new Vector2(-1f, 3f);
                break;
            case Direction.SouthEast:
                westTankPosition += new Vector2(-3f, 0.5f);
                eastTankPosition += new Vector2(0.5f, -3f);
                westPosition += new Vector2(-3f, -1f);
                eastPosition += new Vector2(-1f, -3f);
                break;
            case Direction.SouthWest:
                westTankPosition += new Vector2(-0.5f, -3f);
                eastTankPosition += new Vector2(3f, 0.5f);
                westPosition += new Vector2(1f, -3f);
                eastPosition += new Vector2(3f, -1f);
                break;
            default:
                westTankPosition += new Vector2(-0.5f, 3f);
                eastTankPosition += new Vector2(3f, -0.5f);
                westPosition += new Vector2(1f, 3f);
                eastPosition += new Vector2(3f, -1f);
                break;
        }

        foreach (var stack in Enum.GetValues<WaveStack>())
            if (Controller.TryGetElementByName(stack + nameof(WaveStack), out var element))
            {
                element.Enabled = C.ShowOther;
                element.radius = stack is WaveStack.WestTank or WaveStack.EastTank ? 0.5f : 1.2f;
                element.SetOffPosition(stack switch
                {
                    WaveStack.WestTank => westTankPosition.ToVector3(0),
                    WaveStack.EastTank => eastTankPosition.ToVector3(0),
                    WaveStack.West => westPosition.ToVector3(0),
                    WaveStack.East => eastPosition.ToVector3(0),
                    _ => throw new InvalidOperationException()
                });
            }

        if (Controller.TryGetElementByName(myStack + nameof(WaveStack), out var myElement))
        {
            myElement.Enabled = true;
            myElement.tether = true;
            myElement.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
        }
    }

    private void Split()
    {
        Controller.GetRegisteredElements().Each(x => x.Value.Enabled = false);
        if (C.HighlightSplitPosition && Controller.TryGetElementByName("SplitPosition", out var myElement))
        {
            myElement.Enabled = true;
            myElement.tether = true;
            myElement.color = GradientColor.Get(C.BaitColor1, C.BaitColor2).ToUint();
        }

        Alert(C.SplitText.Get());
    }

    public override void OnSettingsDraw()
    {
        ImGuiEx.Text(EColor.RedBright, """
                                       This script has not been thoroughly tested.
                                       It may not work properly.
                                       If you encounter any bugs, please let us know.
                                       """);
        if (ImGuiEx.CollapsingHeader("General"))
        {
            ImGuiEx.Text("Priority");
            ImGui.Indent();
            ImGui.Text("West");
            C.PriorityData.Draw();
            ImGui.Text("East");
            ImGui.Unindent();
            ImGui.Separator();

            ImGuiEx.EnumCombo("Hit Timing", ref C.HitTiming);
            ImGui.Checkbox("Should Go North When Red Blizzard Hit to Dragon", ref C.ShouldGoNorthRedBlizzard);
            ImGuiEx.HelpMarker(
                "During Red Blizzard, if there is no one in the north, the navigation will appear in the north instead of the south.");
            if (C.ShouldGoNorthRedBlizzard)
            {
                ImGui.Indent();
                ImGui.Checkbox("Automatically use sprint action ~1 seconds", ref C.UseSprintAuto);
                ImGui.Unindent();
            }

            ImGui.Separator();
            ImGuiEx.Text("Sentence Moves");
            ImGui.Indent();
            ImGui.Checkbox("PrioritizeMarker", ref C.PrioritizeMarker);
            if (C.PrioritizeMarker)
            {
                ImGui.Indent();
                ImGui.InputText("Execute Command When Blue Debuff Gained", ref C.CommandWhenBlueDebuff, 30);
                ImGui.Checkbox("Random Wait", ref C.ShouldUseRandomWait);
                if (C.ShouldUseRandomWait)
                {
                    var minWait = C.WaitRange.X;
                    var maxWait = C.WaitRange.Y;
                    ImGui.SliderFloat2("Wait Range (sec)", ref C.WaitRange, 0f, 3f, "%.1f");
                    if (Math.Abs(minWait - C.WaitRange.X) > 0.01f)
                    {
                        if (C.WaitRange.X > C.WaitRange.Y)
                            C.WaitRange.Y = C.WaitRange.X;
                    }
                    else if (Math.Abs(maxWait - C.WaitRange.Y) > 0.01f)
                    {
                        if (C.WaitRange.Y < C.WaitRange.X)
                            C.WaitRange.X = C.WaitRange.Y;
                    }
                }

                ImGui.Separator();
                ImGuiEx.EnumCombo("When Attack 1", ref C.WhenAttack1);
                ImGuiEx.EnumCombo("When Attack 2", ref C.WhenAttack2);
                ImGuiEx.EnumCombo("When Attack 3", ref C.WhenAttack3);
                ImGuiEx.EnumCombo("When Attack 4", ref C.WhenAttack4);
                ImGui.Unindent();
            }

            ImGuiEx.EnumCombo("West Sentence", ref C.WestSentence);
            ImGuiEx.EnumCombo("South West Sentence", ref C.SouthWestSentence);
            ImGuiEx.EnumCombo("South East Sentence", ref C.SouthEastSentence);
            ImGuiEx.EnumCombo("East Sentence", ref C.EastSentence);
            ImGui.Unindent();
            ImGui.Separator();

            ImGui.Checkbox("Highlight static Spirit taker position. ", ref C.HighlightSplitPosition);
            ImGuiEx.TextWrapped(EColor.RedBright,
                "You must go to Registered Elements section and put \"SplitPosition\" element to where you want it to be. Go to Eden's Promise: Eternity undersized for a preview, if necessary.");

            if (C.HighlightSplitPosition)
                if (Controller.TryGetElementByName("SplitPosition", out var element))
                {
                    ImGui.Indent();
                    ImGui.Text($"Position:{element.refX}, {element.refY}");
                    ImGuiEx.EnumCombo("Edit Direction", ref _editSplitElementDirection);
                    ImGui.InputFloat("Edit Radius", ref _editSplitElementRadius, 0.1f);
                    if (ImGui.Button("Set"))
                    {
                        var position = new Vector3(100, 0, 100) + MathHelper.RotateWorldPoint(Vector3.Zero,
                            ((int)_editSplitElementDirection).DegreesToRadians(),
                            -Vector3.UnitZ * _editSplitElementRadius);
                        element.SetRefPosition(position);
                    }

                    ImGui.Unindent();
                }

            ImGui.Separator();

            ImGuiEx.Text("Place Return Moves");
            ImGui.Indent();

            var kbiRewind = C.KBIRewind;
            var nukemaruRewind = C.NukemaruRewind;
            ImGui.Checkbox("Knockback immunity return positions (beta)", ref kbiRewind);
            ImGui.Checkbox("Nukemaru's return positions", ref nukemaruRewind);

            if (!C.KBIRewind && kbiRewind)
                nukemaruRewind = false;
            else if (!C.NukemaruRewind && nukemaruRewind) kbiRewind = false;

            C.KBIRewind = kbiRewind;
            C.NukemaruRewind = nukemaruRewind;

            if (C.NukemaruRewind)
            {
                ImGui.Indent();
                ImGuiEx.EnumCombo("When North East Wave", ref C.NukemaruRewindPositionWhenNorthEastWave);
                ImGuiEx.EnumCombo("When South East Wave", ref C.NukemaruRewindPositionWhenSouthEastWave);
                ImGuiEx.EnumCombo("When South West Wave", ref C.NukemaruRewindPositionWhenSouthWestWave);
                ImGuiEx.EnumCombo("When North West Wave", ref C.NukemaruRewindPositionWhenNorthWestWave);
                ImGui.Unindent();
            }

            if (C is { KBIRewind: false, NukemaruRewind: false })
            {
                ImGui.Checkbox("Is Tank", ref C.IsTank);

                ImGui.Text("When North East Wave:");
                ImGui.SameLine();
                ImGuiEx.RadioButtonBool($"West##{nameof(C.IsWestWhenNorthEastWave)}",
                    $"East##{nameof(C.IsWestWhenNorthEastWave)}", ref C.IsWestWhenNorthEastWave, true);
                ImGui.Text("When South East Wave:");
                ImGui.SameLine();
                ImGuiEx.RadioButtonBool($"West##{nameof(C.IsWestWhenSouthEastWave)}",
                    $"East##{nameof(C.IsWestWhenSouthEastWave)}", ref C.IsWestWhenSouthEastWave, true);
                ImGui.Text("When South West Wave:");
                ImGui.SameLine();
                ImGuiEx.RadioButtonBool($"West##{nameof(C.IsWestWhenSouthWestWave)}",
                    $"East##{nameof(C.IsWestWhenSouthWestWave)}", ref C.IsWestWhenSouthWestWave, true);
                ImGui.Text("When North West Wave:");
                ImGui.SameLine();
                ImGuiEx.RadioButtonBool($"West##{nameof(C.IsWestWhenNorthWestWave)}",
                    $"East##{nameof(C.IsWestWhenNorthWestWave)}", ref C.IsWestWhenNorthWestWave, true);
            }

            ImGui.Unindent();

            ImGui.Separator();

            ImGui.Text("Dialogue Text:");
            ImGui.Indent();
            var splitText = C.SplitText.Get();
            ImGui.Text("Split Text:");
            ImGui.SameLine();
            C.SplitText.ImGuiEdit(ref splitText);

            var hitDragonText = C.HitDragonText.Get();
            ImGui.Text("Hit Dragon Text:");
            ImGui.SameLine();
            C.HitDragonText.ImGuiEdit(ref hitDragonText);

            var avoidWaveText = C.AvoidWaveText.Get();
            ImGui.Text("Avoid Wave Text:");
            ImGui.SameLine();
            C.AvoidWaveText.ImGuiEdit(ref avoidWaveText);

            var cleanseText = C.CleanseText.Get();
            ImGui.Text("Cleanse Text:");
            ImGui.SameLine();
            C.CleanseText.ImGuiEdit(ref cleanseText);

            var placeReturnText = C.PlaceReturnText.Get();
            ImGui.Text("Place Return Text:");
            ImGui.SameLine();
            C.PlaceReturnText.ImGuiEdit(ref placeReturnText);

            ImGui.Unindent();

            ImGui.Separator();
            ImGui.Text("Bait Color:");
            ImGuiComponents.HelpMarker(
                "Change the color of the bait and the text that will be displayed on your bait.\nSetting different values makes it rainbow.");
            ImGui.Indent();
            ImGui.ColorEdit4("Color 1", ref C.BaitColor1, ImGuiColorEditFlags.NoInputs);
            ImGui.SameLine();
            ImGui.ColorEdit4("Color 2", ref C.BaitColor2, ImGuiColorEditFlags.NoInputs);
            ImGui.Unindent();

            ImGui.Separator();
            ImGui.Checkbox("Automatically use KB immunity action ~2 seconds before rewind", ref C.UseKbiAuto);
            ImGui.Checkbox("Automatically use mitigation action ~4 seconds before rewind", ref C.UseMitigation);
            if (C.UseMitigation)
            {
                ImGui.Indent();
                var actions = Ref<Dictionary<uint, string>>.Get(InternalData.FullName + "mitigations",
                    () => Svc.Data.GetExcelSheet<Action>()
                        .Where(x => x.IsPlayerAction && x.ClassJobCategory.RowId != 0 && x.ActionCategory.RowId == 4)
                        .ToDictionary(x => x.RowId, x => x.Name.ExtractText()));
                ImGuiEx.Combo("Select action", ref C.MitigationAction, actions.Keys, names: actions);
                ImGui.Unindent();
            }

            ImGui.Checkbox("Automatically use tank mitigation action ~4 seconds before rewind",
                ref C.UseTankMitigation);
            if (C.UseTankMitigation)
            {
                ImGui.Indent();
                var actions = Ref<Dictionary<uint, string>>.Get(InternalData.FullName + "tankMitigations",
                    () => Svc.Data.GetExcelSheet<Action>()
                        .Where(x => x.IsPlayerAction &&
                                    (x.ClassJobCategory.Value.DRK || x.ClassJobCategory.Value.WAR ||
                                     x.ClassJobCategory.Value.PLD || x.ClassJobCategory.Value.GNB) &&
                                    x.ActionCategory.RowId == 4)
                        .ToDictionary(x => x.RowId, x => x.Name.ExtractText()));
                ImGuiEx.Combo("Select tank action", ref C.TankMitigationAction, actions.Keys, names: actions);
                ImGui.Unindent();
            }

            ImGui.Separator();

            ImGui.Checkbox("Show Other", ref C.ShowOther);

            if (ImGui.CollapsingHeader("Prio list"))
            {
                ImGuiEx.Text(C.PriorityData.GetPlayers(x => true).Select(x => x.NameWithWorld).Print("\n"));
                ImGui.Separator();
                ImGuiEx.Text("Red bliz:");
                ImGuiEx.Text(C.PriorityData.GetPlayers(x => _players.First(y => y.Value.PlayerName == x.Name).Value is
                    { Color: Debuff.Red, Debuff: Debuff.Blizzard }).Select(x => x.NameWithWorld).Print("\n"));
                ImGui.Separator();
                ImGuiEx.Text("Red aero:");
                ImGuiEx.Text(C.PriorityData.GetPlayers(x => _players.First(y => y.Value.PlayerName == x.Name).Value is
                    { Color: Debuff.Red, Debuff: Debuff.Aero }).Select(x => x.NameWithWorld).Print("\n"));
            }
        }

        if (ImGuiEx.CollapsingHeader("Debug"))
        {
            ImGuiEx.Text($"Stage: {GetStage()}, remaining time = {SpellInWaitingDebuffTime}");
            ImGui.SetNextItemWidth(200);
            ImGui.InputText("Player override", ref _basePlayerOverride, 50);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            if (ImGui.BeginCombo("Select..", "Select..."))
            {
                foreach (var x in Svc.Objects.OfType<IPlayerCharacter>())
                    if (ImGui.Selectable(x.GetNameWithWorld()))
                        _basePlayerOverride = x.Name.ToString();
                ImGui.EndCombo();
            }

            ImGui.Text($"Base Direction: {_baseDirection.ToString()}");
            ImGui.Text($"Late Hourglass Direction: {_lateHourglassDirection.ToString()}");
            ImGui.Text($"First Wave Direction: {_firstWaveDirection.ToString()}");
            ImGui.Text($"Second Wave Direction: {_secondWaveDirection.ToString()}");

            ImGuiEx.EzTable("Player Data", _players.SelectMany(x => new ImGuiEx.EzTableEntry[]
            {
                new("Player Name", () => ImGuiEx.Text(x.Value.PlayerName)),
                new("Color", () => ImGuiEx.Text(x.Value.Color.ToString())),
                new("Debuff", () => ImGuiEx.Text(x.Value.Debuff.ToString())),
                new("Has Quietus", () => ImGuiEx.Text(x.Value.HasQuietus.ToString())),
                new("Move Type", () => ImGuiEx.Text(x.Value.MoveType.ToString()))
            }));

            ImGuiEx.EnumCombo("First Wave Direction", ref _debugDirection1);
            ImGuiEx.EnumCombo("Second Wave Direction", ref _debugDirection2);
            if (ImGui.Button("Show Return Placement"))
            {
                _firstWaveDirection = _debugDirection1;
                _secondWaveDirection = _debugDirection2;
            }
        }
    }

    public override void OnActorControl(uint sourceId, uint command, uint p1, uint p2, uint p3, uint p4, uint p5,
        uint p6, ulong targetId,
        byte replaying)
    {
        if (GetStage() == MechanicStage.Unknown) return;
        if (command == 502)
            try
            {
                _players[p2].Marker = (MarkerType)p1;
            }
            catch
            {
                PluginLog.Warning($"GameObjectId:{p2} was not found");
            }
    }

    private Vector2? ResolveRedAeroMove()
    {
        if (_players.SafeSelect(BasePlayer.GameObjectId)?.MoveType?
                .EqualsAny(MoveType.RedAeroEast, MoveType.RedAeroWest) != true) return null;
        var isPlayerWest = _players.SafeSelect(BasePlayer.GameObjectId)?.MoveType == MoveType.RedAeroWest;
        var isLateHourglassSameSide =
            _lateHourglassDirection is Direction.NorthEast or Direction.SouthWest == isPlayerWest;
        var stage = GetStage();
        switch (stage)
        {
            case MechanicStage.Step1_Spread:
                return MirrorX(RedAeroEastMovements.Step1_InitialDodge, isPlayerWest);
            case MechanicStage.Step2_FirstHourglass when isLateHourglassSameSide:
            {
                if (BasePlayer.StatusList.Any(x => x.StatusId == (uint)Debuff.Aero))
                    return MirrorX(RedAeroEastMovements.Step2_KnockPlayers, isPlayerWest);

                Alert(C.HitDragonText.Get());
                return (isPlayerWest ? WestDragon : EastDragon)?.Position.ToVector2();
            }
            case MechanicStage.Step2_FirstHourglass:
                return MirrorX(RedAeroEastMovements.Step3_DodgeSecondHourglass, isPlayerWest);
            case MechanicStage.Step3_IcesAndWinds when isLateHourglassSameSide:
            {
                if (BasePlayer.StatusList.Any(x => x.StatusId == (uint)Debuff.Red))
                {
                    Alert(C.HitDragonText.Get());
                    return (isPlayerWest ? WestDragon : EastDragon)?.Position.ToVector2();
                }

                return MirrorX(RedAeroEastMovements.Step3_DodgeSecondHourglass, isPlayerWest);
            }
            case MechanicStage.Step3_IcesAndWinds:
                return MirrorX(RedAeroEastMovements.Step3_DodgeSecondHourglass, isPlayerWest);
            case MechanicStage.Step4_SecondHourglass when isLateHourglassSameSide:
            {
                if (BasePlayer.StatusList.Any(x => x.StatusId == (uint)Debuff.Red))
                {
                    Alert(C.HitDragonText.Get());
                    return (isPlayerWest ? WestDragon : EastDragon)?.Position.ToVector2();
                }

                return MirrorX(RedAeroEastMovements.Step3_DodgeSecondHourglass, isPlayerWest);
            }
            case MechanicStage.Step4_SecondHourglass:
                return MirrorX(RedAeroEastMovements.Step3_DodgeSecondHourglass, isPlayerWest);
            case MechanicStage.Step5_PerformDodges when BasePlayer.StatusList.Any(x => x.StatusId == (uint)Debuff.Red):
                Alert(C.HitDragonText.Get());
                return (isPlayerWest ? WestDragon : EastDragon)?.Position.ToVector2();
            case MechanicStage.Step5_PerformDodges:
                return MirrorX(RedAeroEastMovements.Step3_DodgeSecondHourglass, isPlayerWest);
            default:
                return null;
        }
    }

    private Vector2? ResolveRedBlizzardMove()
    {
        if (_players.SafeSelect(BasePlayer.GameObjectId)?.MoveType?.EqualsAny(MoveType.RedBlizzardWest,
                MoveType.RedBlizzardEast) != true) return null;
        var isPlayerWest = _players.SafeSelect(BasePlayer.GameObjectId)?.MoveType == MoveType.RedBlizzardWest;
        var isLateHourglassSameSide =
            (_lateHourglassDirection == Direction.NorthEast || _lateHourglassDirection == Direction.SouthWest) ==
            isPlayerWest;
        var stage = GetStage();
        if (stage <= MechanicStage.Step5_PerformDodges)
        {
            if (BasePlayer.StatusList.Any(x => x.StatusId == (uint)Debuff.Red)) return null;
            if (isLateHourglassSameSide)
            {
                if (stage <= MechanicStage.Step4_SecondHourglass && !C.ShouldGoNorthRedBlizzard)
                    return MirrorX(new Vector2(119, 103), isPlayerWest);
                return MirrorX(new Vector2(105, 82), isPlayerWest);
            }

            return MirrorX(new Vector2(105, 82), isPlayerWest);
        }

        return null;
    }

    private static Vector2 MirrorX(Vector2 x, bool mirror)
    {
        if (mirror)
            return x with { X = 100f - Math.Abs(x.X - 100f) };
        return x;
    }

    private enum Debuff : uint
    {
        Red = 0xCBF,
        Blue = 0xCC0,
        Holy = 0x996,
        Eruption = 0x99C,
        Water = 0x99D,
        Blizzard = 0x99E,
        Aero = 0x99F,
        Quietus = 0x104E,
        DelayReturn = 0x1070,
        Return = 0x994
    }


    private enum HitTiming
    {
        Early,
        Late
    }

    private enum MarkerType : uint
    {
        Attack1 = 0,
        Attack2 = 1,
        Attack3 = 2,
        Attack4 = 3
    }

    private enum MoveType
    {
        RedBlizzardWest,
        RedBlizzardEast,
        RedAeroWest,
        RedAeroEast,
        BlueBlizzard,
        BlueHoly,
        BlueWater,
        BlueEruption
    }

    private enum WaveStack
    {
        WestTank,
        EastTank,
        West,
        East
    }

    private enum MechanicStage
    {
        Unknown,

        /// <summary>
        ///     Tethers appear, red winds and red ices go to their designated positions, eruption goes front, other blues go back
        /// </summary>
        Step1_Spread,

        /// <summary>
        ///     First set of hourglass goes off, winds go to their positions, ice prepares to pop dragon heads, and blue people in
        ///     back go to winds to be knocked
        /// </summary>
        Step2_FirstHourglass,

        /// <summary>
        ///     Winds and ices now went off. Party in back gets knocked to front; ices must now dodge hourglasses and rejoin the
        ///     group in front, while winds must prepare to pop their dragon heads.
        /// </summary>
        Step3_IcesAndWinds,

        /// <summary>
        ///     Second set of hourglass goes off. Winds must immediately intercept dragon heads if early pop is selected, otherwise
        ///     they wait for third set of hourglass at south.
        /// </summary>
        Step4_SecondHourglass,

        /// <summary>
        ///     Stack in front now resolved, and blue people can perform their dodges.
        /// </summary>
        Step5_PerformDodges,

        /// <summary>
        ///     Third set of hourglass goes off. Blue people must cleanse now. Red already prepares to drop their rewinds, and once
        ///     blues cleanse, they too prepare to drop their rewinds.
        /// </summary>
        Step6_ThirdHourglass,

        /// <summary>
        ///     Players must now spread for spirit taker bait, press mitigations and kb immunity appropriately if needed
        /// </summary>
        Step7_SpiritTaker
    }


    private record PlayerData
    {
        public Debuff? Color;
        public Debuff? Debuff;
        public bool HasQuietus;
        public MarkerType? Marker;
        public MoveType? MoveType;
        public string PlayerName;

        public bool HasDebuff => Debuff != null && Color != null;
    }

    private static class RedAeroEastMovements
    {
        public static Vector2 Step1_InitialDodge = new(112, 115);
        public static Vector2 Step2_KnockPlayers = new(109.9f, 117); //only when purple hourglass on our side
        public static Vector2 Step3_DodgeSecondHourglass = new(107.8f, 117.9f);
        public static Vector2 Step4_DodgeExa = new(100, 117);
    }

    private class Config : IEzConfig
    {
        public InternationalString AvoidWaveText = new() { En = "Avoid Wave", Jp = "波をよけろ！" };
        public Vector4 BaitColor1 = 0xFFFF00FF.ToVector4();
        public Vector4 BaitColor2 = 0xFFFFFF00.ToVector4();
        public InternationalString CleanseText = new() { En = "Get Cleanse", Jp = "白を取れ！" };
        public string CommandWhenBlueDebuff = "";
        public MoveType EastSentence = MoveType.BlueBlizzard;

        public bool HighlightSplitPosition;

        public InternationalString HitDragonText = new() { En = "Hit Dragon", Jp = "竜に当たれ！" };

        public HitTiming HitTiming = HitTiming.Late;

        public bool IsTank;
        public bool IsWestWhenNorthEastWave;
        public bool IsWestWhenNorthWestWave;
        public bool IsWestWhenSouthEastWave;
        public bool IsWestWhenSouthWestWave;

        public bool KBIRewind;
        public uint MitigationAction;

        public bool NoWindWait = false;
        public bool NukemaruRewind;

        public Direction NukemaruRewindPositionWhenNorthEastWave = Direction.North;
        public Direction NukemaruRewindPositionWhenNorthWestWave = Direction.North;
        public Direction NukemaruRewindPositionWhenSouthEastWave = Direction.South;
        public Direction NukemaruRewindPositionWhenSouthWestWave = Direction.South;
        public InternationalString PlaceReturnText = new() { En = "Place Return", Jp = "リターンを置け！" };

        public bool PrioritizeMarker;

        public PriorityData PriorityData = new();

        public bool ShouldGoNorthRedBlizzard;

        public bool ShouldUseRandomWait = true;


        public bool ShowOther;
        public MoveType SouthEastSentence = MoveType.BlueHoly;
        public MoveType SouthWestSentence = MoveType.BlueWater;
        public InternationalString SplitText = new() { En = "Split", Jp = "散開！" };
        public uint TankMitigationAction;
        public bool UseKbiAuto;
        public bool UseMitigation;
        public bool UseSprintAuto;
        public bool UseTankMitigation;
        public Vector2 WaitRange = new(0.5f, 1.5f);
        public MoveType WestSentence = MoveType.BlueEruption;
        public Direction WhenAttack1 = Direction.East;
        public Direction WhenAttack2 = Direction.SouthEast;
        public Direction WhenAttack3 = Direction.SouthWest;
        public Direction WhenAttack4 = Direction.West;
    }
}
```

* スクリプトの設定(Configuration)  
  * General  
    ↑が西↓が東で赤ブリとエアロガの優先度を設定する。  
  * Hit Timing  
    エアロガ持ちが竜の頭にぶつかるタイミングの設定。  
    竜の頭が南で交差する前に竜の頭にぶつかる場合は`Early`に設定する。  
    竜の頭が南で交差した後に竜の頭にぶつかる場合は`Late`に設定する。  
    * Should Go North When Red Blizzard Hit to Dragon  
      赤ブリ担当(北にエラプがいない)が竜の頭にぶつかった後、スプリントを炊いて北に爆走する場合はチェックする。  
      そうでない場合(一旦時計のAoEを回避してから北上する場合)はチェックしない。  
  * Sentence Moves  
    * Prioritize Marker  
      青デバフ持ちが解除する場所を、ターゲットマーカーで決める場合はチェックする。  
      そうでない場合(自分のデバフの種類で決める場合)はチェックしない。  
      * Execute Command When Blue Debuff Gained  
        自分が青デバフだった時にマーカー付与を実行する為のコマンドを設定する。  
        自分が青デバフだった時、自分に攻撃マーカーを付与する場合は`/mk attack <me>`  
      * When Attack 1-4  
        自分についている攻撃マーカーの番号と赤デバフを解除しにいく方角の対応関係を設定する。  
    * West Sentence  
    * South West Sentence  
    * South East Sentence  
    * East Sentence  
      `Prioritize Marker`にチェックを入れなかった場合(自分についたデバフの種類で赤デバフを解除する方角を決める場合)の設定。  
      赤デバフ解除の方角とデバフの対応関係を設定する。  
  * Highlight static Spirit taker position  
    時間結晶フェーズでの、スピリットテイカーの散開位置を表示する。  
    有効にしたい場合はチェックを入れる。  
  * Place Return Moves  
    リターン設置の設定  
    処理方法に該当しない場合は、チェックしない。  
    ぬけまる式で処理する場合や、自分がタンクである場合は該当箇所をチェックする。  
    * Knock back immunity return positions(beta)  
      アムレン処理をする場合のリターン設置位置  
      ※ぬけまるとは別です。  
    * Nukemaru's return positions  
      ぬけまる式のY時アムレン処理のリターン設置位置で処理する場合はチェックする。  
    * Is Tank  
      自分がタンクである場合(タンクの立ち位置にリターンを設置する場合)はチェックする。  
    * When North East Wave  
    * When South East Wave  
    * When South West Wave  
    * When North West Wave  
      光の大波のパターンに対応した自分の担当の位置をチェックする。  
  * Dialogue Text  
    処理毎のメッセージを設定する。  
  * Bait Color  
    誘導表示の色を設定  
    お好みで  
    Color1:`#00FF00FF`  
    Color2:`#00FF00FF`  
  * Automatically use KB immunity action ~2 seconds before rewind  
    リターン発動2秒前にアムレンを自動的に使用する。  
    アムレン処理で有効にしたい場合はチェック。  
  * Automatically use mitigation action ~4 seconds before rewind  
    リターン発動4秒前に自動的に軽減スキルを使用する。  
    使用する場合は、チェックを入れた後に自動的に使用したいスキルを設定する。  
    * Select action  
      `Automatically use mitigation action ~4 seconds before rewind`にチェックを入れている場合にのみ表示される。  
      自動的に使用したい軽減スキルを設定。  
  * Automatically use tank mitigation action ~4 seconds before rewind  
    リターン発動4秒前に自動的にタンクの軽減スキルを使用する。  
    使用する場合は、チェックを入れた後に自動的に使用したいスキルを設定する。  
    * Select tank action  
      `Automatically use tank mitigation action ~4 seconds before rewind`にチェックを入れている場合にのみ表示される。  
      自動的に使用したい軽減スキルを設定。  
* スクリプトの設定(Registered Elements)  
  全ての誘導表示の塗りつぶし(Fill)のチェックを外しています。  
  また、alert(テキスト表示)による赤いdotを非表示にしています。  
  お好みで  

```
{"Elements":{"RedBlizzardWest":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":88.0,"offY":86.0,"offZ":0.0,"radius":1.0,"color":4278190250,"Filled":false,"fillIntensity":0.39215687,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"RedBlizzardEast":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":88.0,"offY":86.0,"offZ":0.0,"radius":1.0,"color":4278190250,"Filled":false,"fillIntensity":0.39215687,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"RedAeroWest":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":93.0,"offY":118.0,"offZ":0.0,"radius":1.0,"color":4278190250,"Filled":false,"fillIntensity":0.39215687,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"RedAeroEast":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":100.0,"offY":115.0,"offZ":0.0,"radius":1.0,"color":4278190250,"Filled":false,"fillIntensity":0.39215687,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"BlueBlizzard":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":88.0,"offY":86.0,"offZ":0.0,"radius":1.0,"color":4278190250,"Filled":false,"fillIntensity":0.39215687,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"BlueHoly":{"Name":"","type":0,"Enabled":true,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":88.0,"offY":86.0,"offZ":0.0,"radius":1.0,"color":4279172864,"Filled":false,"fillIntensity":0.39215687,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":true,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"BlueWater":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":88.0,"offY":86.0,"offZ":0.0,"radius":1.0,"color":4278190250,"Filled":false,"fillIntensity":0.39215687,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"BlueEruption":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":88.0,"offY":86.0,"offZ":0.0,"radius":1.0,"color":4278190250,"Filled":false,"fillIntensity":0.39215687,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"WestTankWaveStack":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.5,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"EastTankWaveStack":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.5,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"WestWaveStack":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.5,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"EastWaveStack":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.5,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"Alert":{"Name":"","type":1,"Enabled":false,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.0,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":1.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":0.0,"overlayText":"波をよけろ！","refActorTargetingYou":0,"refActorPlaceholder":["<1>"],"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":5,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"SplitPosition":{"Name":"","type":0,"Enabled":false,"refX":100.0,"refY":100.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":1.0,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":4.0,"overlayText":"Spread!","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"KBHelper":{"Name":"","type":2,"Enabled":false,"refX":110.15369,"refY":116.48472,"refZ":-0.0012040206,"offX":88.0,"offY":85.0,"offZ":0.0,"radius":0.0,"color":3355508503,"Filled":false,"fillIntensity":0.345,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":4.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"RedDragonExplosion1":{"Name":"","type":0,"Enabled":false,"refX":87.5,"refY":98.0,"refZ":1.9073486E-06,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":13.0,"color":3372155112,"Filled":true,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":2.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"RedDragonExplosion2":{"Name":"","type":0,"Enabled":false,"refX":112.5,"refY":98.0,"refZ":1.9073486E-06,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":13.0,"color":3372155112,"Filled":true,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":2.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0}}}
```


### P5 Fulgent blade dodge spots  

公式から、エクサ回避する為の安置への行先を表示する。  
設定不要  

```
https://github.com/PunishXIV/Splatoon/raw/refs/heads/main/SplatoonScripts/Duties/Dawntrail/The%20Futures%20Rewritten/P5%20Fulgent%20Blade%20Dodge.cs
```


### P5 Paradise Regained  

公式から、タンク強の誘導や塔踏み位置を表示する。  

* スクリプトの設定(Configuration)  
  * MoveType  
  * TowerType,FirstBaitType,SecondBaitType  
    設定によって項目名が切り替わります。  
    一つ目の設定が`MoveType`  
    二つ目の設定が`TowerType` or `FirstBaitType` or `Second Bait Type`です。  
    * ぬけまるの場合  
      | 項目名 | MT | ST | PH | BH | D1 | D2 | D3 | D4 |  
      | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- |  
      | **1. MoveType** | FirstBait | SecondBait | Tower | Tower | Tower | Tower | Tower | Tower |  
      | **2. TowerType** | - | - | First | First | SecondSafe | SecondSafe | FirstSafe | FirstSafe |  
      | **2. First Bait Type** | GoToPppositeFirstTower | - | - | - | - | - | - | - |  
      | **2. Second Bait Type** | - | GoToSafe | - | - | - | - | - | - |  
    * リリドの場合  
      | 項目名 | MT | ST | PH | BH | D1 | D2 | D3 | D4 |  
      | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- | ---- |  
      | **1. MoveType** | FirstBait | SecondBait | Tower | Tower | Tower | Tower | Tower | Tower |  
      | **2. TowerType** | - | - | First | First | Right | Right | Left | Left |  
      | **2. First Bait Type** | GoToPppositeFirstTower | - | - | - | - | - | - | - |  
      | **2. Second Bait Type** | - | GoToSafe | - | - | - | - | - | - |  
  * BaitColor  
    誘導色の変更。お好みで  
    `Color1`を`#00FF00FF`  
    `Color2`を`#00FF00FF`  
  * Show Predict  
    予測表示を行うかの設定  
    お好みで、個人的にはチェックを外す  
  * Show Tank AOE  
    タンクのAoE（円範囲）の表示  
    お好みで、個人的にはチェックを入れる  
* スクリプトの設定(Registered elements)  
  行先がわかりやすいように、誘導する円の表示のテザーのチェックを全て入れています。  
  お好みで導入してください。  
```
{"Elements":{"Tower":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":3.0,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":3.0,"overlayFScale":3.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"<< Go Here >>","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":true,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"PredictTower":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":3.0,"color":4278190335,"Filled":false,"fillIntensity":0.39215687,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":3.0,"overlayFScale":3.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"Your Tower","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":true,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"Bait":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.5,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":3.0,"overlayFScale":3.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"<< Go Here >>","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":true,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"PredictBait":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":0.5,"color":3355443455,"Filled":false,"fillIntensity":0.5,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":3.0,"overlayFScale":3.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"<< Next >>","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":true,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0},"TankAOE":{"Name":"","type":0,"Enabled":false,"refX":0.0,"refY":0.0,"refZ":0.0,"offX":0.0,"offY":0.0,"offZ":0.0,"radius":5.0,"color":4278190335,"Filled":true,"fillIntensity":0.102,"overlayBGColor":1879048192,"overlayTextColor":3372220415,"overlayVOffset":0.0,"overlayFScale":1.0,"overlayPlaceholders":false,"thicc":6.0,"overlayText":"","refActorName":"","refActorTargetingYou":0,"refActorNamePlateIconID":0,"refActorComparisonAnd":false,"refActorRequireCast":false,"refActorCastReverse":false,"refActorUseCastTime":false,"refActorCastTimeMin":0.0,"refActorCastTimeMax":0.0,"refActorUseOvercast":false,"refTargetYou":false,"refActorRequireBuff":false,"refActorRequireAllBuffs":false,"refActorRequireBuffsInvert":false,"refActorUseBuffTime":false,"refActorUseBuffParam":false,"refActorBuffTimeMin":0.0,"refActorBuffTimeMax":0.0,"refActorObjectLife":false,"refActorComparisonType":0,"refActorType":0,"includeHitbox":false,"includeOwnHitbox":false,"includeRotation":false,"onlyTargetable":false,"onlyUnTargetable":false,"onlyVisible":false,"tether":false,"ExtraTetherLength":0.0,"LineEndA":0,"LineEndB":0,"AdditionalRotation":0.0,"LineAddHitboxLengthX":false,"LineAddHitboxLengthY":false,"LineAddHitboxLengthZ":false,"LineAddHitboxLengthXA":false,"LineAddHitboxLengthYA":false,"LineAddHitboxLengthZA":false,"LineAddPlayerHitboxLengthX":false,"LineAddPlayerHitboxLengthY":false,"LineAddPlayerHitboxLengthZ":false,"LineAddPlayerHitboxLengthXA":false,"LineAddPlayerHitboxLengthYA":false,"LineAddPlayerHitboxLengthZA":false,"FaceMe":false,"LimitDistance":false,"LimitDistanceInvert":false,"DistanceSourceX":0.0,"DistanceSourceY":0.0,"DistanceSourceZ":0.0,"DistanceMin":0.0,"DistanceMax":0.0,"LimitRotation":false,"refActorTether":false,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorTetherParam1":null,"refActorTetherParam2":null,"refActorTetherParam3":null,"refActorIsTetherSource":null,"refActorIsTetherInvert":false,"refActorUseTransformation":false,"mechanicType":0,"refMark":false,"refMarkID":0,"faceplayer":"<1>","FillStep":0.5,"LegacyFill":false,"RenderEngineKind":0}}}
```

## その他の導入推奨プラグイン  

### Lemegeton  

#### P1 Fall of Faith automarker  

`Futures Rewritten(Ultimate)`と`(P1)Fall of Faith automarker`の`Enabled`にチェック  
`歯車マーク`をクリックして、`Self-marking only`と`Show as client-side soft markers`にチェック  
設定はお好みですが、無職の優先度はある程度自分で判断しつつ（自キャラを含む2～4名を見ておく） + Presetの黄色い無職テザーの方が迷いなく動けると思いました。  
無職の時の優先度をマーカー付与されるのはありがたいのですが、このマーカーを見て判断すると散開or頭割りが抜けやすい為です。  
線が自分についた時の視認性の問題の解決と番号を正確に処理する手段として、このプラグインを使うのが良いと思います。  
その為、個人的なおすすめの設定は  
* Tether1～Tether4  
  攻撃1～攻撃4のマーカー  
* Overflow1～Overflow4  
  なし  


## Preset  

### 選択項目  
いくつかのプリセットは、複数の細かい処理方法の違いに対応しています。  
処理方法に応じて、必要なものだけをチェックしてください。  

* P2-光の暴走AoE捨てルート  
  光の暴走フェーズにて、AoE担当が1回目のAoEを最初に捨てる位置によって表示するプリセットを変更します。  
  南北スタートは内側の南北に1回目のAoEを捨てる場合のプリセットです。2回目のAoEは南側の人が南西(3マーカー)へ、北側の人が北東(1マーカー)へ捨てる事を想定しています。  
  リリドは南西(3マーカー方面)と北東(1マーカー方面)の内側に1回目のAoEを捨てる場合のプリセットです。2回目のAoEは南側の人が南西(3マーカー)へ、北側の人が北東(1マーカー)へ捨てる事を想定しています。  
  用途に応じて下記のプリセットのうち、どちらか一つにチェックしてください。  
  * P2_シヴァ・ミトロン：AoE捨てルート(南北スタート)  
    光の暴走のAoE担当の最初の位置が南北の内側である場合はこちらをオンにして、リリドをオフにする。  
  * P2_シヴァ・ミトロン：AoE捨てルート(リリド)  
    光の暴走のAoE担当の最初の位置が1,3マーカー方面の内側である場合はこちらをオンにして、南北スタートをオフにする。  


* P3-アポカリプス 初期位置  
  アポカリプスフェーズ前に整列する位置を表示します。  
  TH組は緑の円、DPS組は赤色の円で整列位置の目安が表示されます。  
  また、全員が表示される円の中に入っていれば、表示される円の中でどれだけ離れていても頭割りに参加出来ます。  
  （整列位置の一つを頭割りの範囲の6mにしてみるとわかります。）  
  頭割りの範囲を5mとしてギリギリ収まる形に設定していたため、実際には1m程度余裕があります。その為、少し円から出ていても頭割りに参加出来ます。  
  固定1-3は、北東（1マーカー）周辺にTH、南西(3マーカー)周辺にDPSの整列目安が表示されます。  
  しのしょーは、北東と東の間(1とBマーカーの間)にTH、南西と西の間(3とDマーカーの間)にDPSの整列目安が表示されます。  
  リリドは、北と北西の間(Aと4マーカーの間)にTH、南と南東の間(Cと2マーカーの間)にDPSの整列目安が表示されます。  
  用途に応じて下記のプリセットのうち、どれか一つにチェックしてください。  
  * P3_アポカリプス：初期位置(固定1-3)  
    アポカリ前整列が1マーカー周辺と3マーカー周辺である場合はこちらをオンにして、他(しのしょー,リリド)をオフにする。  
  * P3_アポカリプス：初期位置(しのしょー)  
    アポカリ前整列が1-Bの間と3-Dの間である場合はこちらをオンにして、他(固定1-3,リリド)をオフにする。  
  * P3_アポカリプス：初期位置(リリド)  
    アポカリ前整列が4-Aの間と2-Cの間である場合はこちらをオンにして、他(固定1-3,しのしょー)をオフにする。  
    ※ 2025-02-14 20:07:00 時点で記載されているアポカリ基準、安置基準のどちらのリリドマクロにも対応しています。  
    `/p 頭割り1回目：TH組 4-A間/DPS組 2-C間`  
    今後更新される可能性もあると思うので、使用する際はよくマクロを確認してください。  


* P4-光と闇の竜詩 初期位置  
  光と闇の竜詩フェーズ前に整列する位置を表示します。  
  攻略法に合わせてどちらか一つにチェックしてください。  
  * P4_光と闇の竜詩：初期位置(しのしょー)  
    TH組が西、DPS組が東に別れる。  
    また、 北←MTSTH1H2→南 と 北←D1D2D3D4→南 に整列する場合に使用する。  
    この場合の初期位置は、北側は近接組、南側は遠隔組になる。  
  * P4_光と闇の竜詩：初期位置(リリド)  
    TH組が北、DPS組が南に別れる。  
    また、 西←MTSTH1H2→東 と 西←D1D2D3D4→東 に整列する場合に使用する。  
    この場合の初期位置は、西側は近接組、東側は遠隔組になる。  

* P4-光と闇の竜詩 スピリットテイカー散開  
  光と闇の竜詩フェーズでの、スピリットテイカーの散開位置(プレイヤー同士は5mを超える距離を空ける必要がある)を表示します。  
  初期状態では、余裕を持たせた無難な立ち位置`光と闇の竜詩：テイカー散開位置`を表示します。  
  * 光と闇の竜詩：テイカー散開位置  
    無難な散開位置を表示します。  
    これで表示される場所に立つ場合プレイヤー同士の距離は、6.3～8m程度です。  
    野良や散開位置を予め決めていない固定ではこの設定を使用する事をお勧めします。  
  * 光と闇の竜詩：テイカー最小散開位置  
    扇範囲を誘導するプレイヤーを、可能な限り中央に寄せた散開位置を表示します。  
    プレイヤー同士の距離は、5.3～8m程度です。  
    散開位置を8人全員で共有している場合に限り、使用する事をお勧めします。  

* P4_時間結晶：デバフ解除_マーカー処理_1234->B23D  
  マーカー処理をする場合のプリセットです。  
  青デバフ組が北に集合したタイミングで行先の目安となる表示がされます。  
  デバフで青デバフ解除の担当位置を決める場合は無効にしてください。  

### 補足説明  

* 時間圧縮・絶について  
  時間圧縮・絶はStep.0からStep.7までの合計8Stepで構成されています。  
  Step毎の時間は5秒です。  
  Step.0は自身に付与されたデバフから担当の方角を判断する5秒間。  
  Step.7は3秒程度スタンしてリターン位置に戻された後、ボスの頭割りを受けるまでの5秒間です。  
  PresetではStep.1からStep.6まで表示され、5秒間のカウントダウンがされます。  
  * Step.1 Step.3 Step.5  
    各種デバフ処理  
    ファイガは外周  
    ファイガ以外は中央  
  * Step.2 Step.4  
    リターン設置  
  * Step.2 Step.4 Step.6  
    砂時計のAoEを誘導  
  
  これらをまとめると、Step.1からStep.6までの中で  
  * 奇数のStep(1,3,5)  
    各種デバフ処理。ファイガが外周へ移動。その他は中央。  
  * 偶数のStep(2,4,6)  
    リターンの設置（エラプ持ちは時計の足元、視線持ちは中央）と砂時計AoEの誘導。  
  
  PresetやScriptに表示された通りに移動すれば問題なく処理できます（あるいは自分でデバフを見る方が楽だと思います）が、これを覚えておくとより安心して処理が出来るようになります。  
  また、外周付近へ移動する人が出るのは奇数のStepのみです。ヒールや軽減の範囲を考慮する時に、全員の立ち位置の把握にも役に立つかもしれません。  
  このギミックで一番忘れやすい砂時計の誘導については、大きくメッセージが表示されます。  

### Preset  

[WIP] 現在作成中です。  
[JP/EN]トリガーや敵の名前、キャストID等の設定は英語と日本語に対応しています。  
※ただし、表示されるメッセージは日本語である事に注意してください。  

```
~Lv2~{"Name":"P1_フェイトブレイカー：サイクロニックブレイク_焔","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"ペア割り","type":1,"radius":5.5,"Donut":0.5,"color":4294967040,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4294967040,"thicc":5.0,"overlayText":"2 Stack","refActorComparisonType":1,"refActorType":1,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":9.0,"Match":"(9707>40144)"}]}
~Lv2~{"Name":"P1_フェイトブレイカー：サイクロニックブレイク_雷","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"散開(自分)","type":1,"radius":6.0,"color":4278779648,"Filled":false,"fillIntensity":0.11,"overlayBGColor":3355443200,"overlayTextColor":4278779648,"thicc":4.0,"overlayText":"Spread","refActorType":1,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"散開(その他)","type":1,"radius":6.0,"color":4278779648,"fillIntensity":0.11,"overlayBGColor":3355443200,"overlayTextColor":4278779648,"thicc":4.0,"overlayText":"Spread","refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":9.0,"Match":"(9707>40148)"}],"MaxDistance":7.5,"UseDistanceLimit":true,"DistanceLimitType":1}
~Lv2~{"Name":"P1_フェイトブレイカー：連鎖爆印","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"範囲","type":1,"radius":10.0,"color":3356884736,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorPlaceholder":["<t1>","<t2>"],"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"テザー","type":1,"Enabled":false,"radius":0.0,"color":3372155392,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorPlaceholder":["<t1>","<t2>"],"refActorUseBuffTime":true,"refActorBuffTimeMax":5.0,"refActorComparisonType":5,"tether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":9.0,"Match":"(9707>40168)","MatchDelay":11.7}]}
~Lv2~{"Name":"P1_フェイトブレイカー：連鎖爆印_CD9","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"9","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508490,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"9","refActorPlaceholder":["<t1>","<t2>"],"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9707>40168)","MatchDelay":11.7}]}
~Lv2~{"Name":"P1_フェイトブレイカー：連鎖爆印_CD8","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"8","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508490,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"8","refActorPlaceholder":["<t1>","<t2>"],"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9707>40168)","MatchDelay":12.7}]}
~Lv2~{"Name":"P1_フェイトブレイカー：連鎖爆印_CD7","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"7","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508490,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"7","refActorPlaceholder":["<t1>","<t2>"],"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9707>40168)","MatchDelay":13.7}]}
~Lv2~{"Name":"P1_フェイトブレイカー：連鎖爆印_CD6","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"6","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508490,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"6","refActorPlaceholder":["<t1>","<t2>"],"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9707>40168)","MatchDelay":14.7}]}
~Lv2~{"Name":"P1_フェイトブレイカー：連鎖爆印_CD5","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508490,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"5","refActorPlaceholder":["<t1>","<t2>"],"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9707>40168)","MatchDelay":15.7}]}
~Lv2~{"Name":"P1_フェイトブレイカー：連鎖爆印_CD4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508490,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"4","refActorPlaceholder":["<t1>","<t2>"],"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9707>40168)","MatchDelay":16.7}]}
~Lv2~{"Name":"P1_フェイトブレイカー：連鎖爆印_CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508490,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorPlaceholder":["<t1>","<t2>"],"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9707>40168)","MatchDelay":17.7}]}
~Lv2~{"Name":"P1_フェイトブレイカー：連鎖爆印_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508223,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorPlaceholder":["<t1>","<t2>"],"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9707>40168)","MatchDelay":18.7}]}
~Lv2~{"Name":"P1_フェイトブレイカー：連鎖爆印_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorPlaceholder":["<t1>","<t2>"],"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9707>40168)","MatchDelay":19.7}]}
~Lv2~{"Name":"P1_フェイトブレイカー：楽園絶技_直線AOE","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"楽園絶技_外村","type":3,"refY":40.0,"radius":8.0,"fillIntensity":0.095,"thicc":4.0,"refActorNPCNameID":9708,"refActorComparisonType":6,"includeRotation":true,"tether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refActorUseTransformation":true,"refActorTransformationID":4}]}
~Lv2~{"Name":"P1_フェイトブレイカー：楽園絶技_焔","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"サークル","type":1,"radius":5.5,"Donut":0.5,"color":4294967040,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4294967040,"thicc":5.0,"overlayText":"Stack","refActorPlaceholder":["<h1>","<h2>"],"refActorComparisonType":5,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":10.0,"Match":"(9707>40154)","MatchDelay":10.0}]}
~Lv2~{"Name":"P1_フェイトブレイカー：楽園絶技_雷","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"サークル","type":1,"radius":5.0,"color":4278255376,"fillIntensity":0.108,"overlayBGColor":3355443200,"overlayTextColor":4278255376,"thicc":4.0,"overlayText":"Spread","refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":10.0,"Match":"(9707>40155)","MatchDelay":10.0}],"MaxDistance":7.5,"UseDistanceLimit":true,"DistanceLimitType":1}
~Lv2~{"Name":"P1_楽園絶技：外周強調","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"外周","refX":100.0,"refY":100.0,"refZ":1.9073486E-06,"radius":20.0,"color":4294908672,"Filled":false,"fillIntensity":0.5,"thicc":10.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":20.0,"Match":"(9707>40154)"},{"Type":2,"Duration":20.0,"Match":"(9707>40155)"}]}
~Lv2~{"Name":"P1_フェイトブレイカー：バーンストライク","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"雷","type":3,"refY":30.0,"offY":-30.0,"radius":10.0,"color":4294918144,"Filled":false,"fillIntensity":0.103,"thicc":4.0,"refActorNPCNameID":9707,"refActorRequireCast":true,"refActorCastId":[40133],"refActorUseCastTime":true,"refActorCastTimeMax":7.9,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":2.0},{"Name":"雷(2.5秒前)","type":3,"refY":30.0,"offY":-30.0,"radius":10.0,"color":4294918144,"fillIntensity":0.298,"thicc":0.1,"refActorNPCNameID":9707,"refActorRequireCast":true,"refActorCastId":[40133],"refActorUseCastTime":true,"refActorCastTimeMin":5.4,"refActorCastTimeMax":7.9,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"炎","type":3,"refY":30.0,"offY":-30.0,"radius":5.0,"color":4278190335,"Filled":false,"fillIntensity":0.104,"thicc":4.0,"refActorNPCNameID":9707,"refActorRequireCast":true,"refActorCastId":[40129],"refActorCastTimeMax":3.2,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":2.0},{"Name":"炎(2.5秒前)","type":3,"refY":30.0,"offY":-30.0,"radius":5.0,"color":4278190335,"fillIntensity":0.304,"thicc":0.0,"refActorNPCNameID":9707,"refActorRequireCast":true,"refActorCastId":[40129],"refActorUseCastTime":true,"refActorCastTimeMin":3.7,"refActorCastTimeMax":6.2,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"炎(KB)","type":3,"refY":-30.0,"offY":30.0,"radius":0.0,"fillIntensity":0.345,"thicc":4.0,"refActorNPCNameID":9707,"refActorRequireCast":true,"refActorCastId":[40129],"refActorUseCastTime":true,"refActorCastTimeMin":6.2,"refActorCastTimeMax":9.2,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"KB_中央","type":3,"refX":15.0,"offX":-15.0,"radius":0.0,"fillIntensity":0.345,"thicc":4.0,"refActorNPCNameID":9707,"refActorRequireCast":true,"refActorCastId":[40129],"refActorUseCastTime":true,"refActorCastTimeMin":5.2,"refActorCastTimeMax":9.2,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"KB_前","type":3,"refX":15.0,"refY":9.0,"offX":-15.0,"offY":9.0,"radius":0.0,"fillIntensity":0.345,"thicc":4.0,"refActorNPCNameID":9707,"refActorRequireCast":true,"refActorCastId":[40129],"refActorUseCastTime":true,"refActorCastTimeMin":5.2,"refActorCastTimeMax":9.2,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"KB_後","type":3,"refX":15.0,"refY":-9.0,"offX":-15.0,"offY":-9.0,"radius":0.0,"fillIntensity":0.345,"thicc":4.0,"refActorNPCNameID":9707,"refActorRequireCast":true,"refActorCastId":[40129],"refActorUseCastTime":true,"refActorCastTimeMin":5.2,"refActorCastTimeMax":9.2,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
~Lv2~{"Name":"P1_フェイトブレイカー：シンフレイム_ロール表示","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"TANK","type":1,"radius":0.02,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":4294573824,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"TANK","refActorPlaceholder":["<t1>","<t2>"],"refActorRequireBuff":true,"refActorBuffId":[1051],"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"HEALER","type":1,"radius":0.02,"overlayBGColor":3355443200,"overlayTextColor":4278255420,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"HEALER","refActorPlaceholder":["<h1>","<h2>"],"refActorRequireBuff":true,"refActorBuffId":[1051],"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"DPS","type":1,"radius":0.02,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":4278190335,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"DPS","refActorPlaceholder":["<d1>","<d2>","<d3>","<d4>"],"refActorRequireBuff":true,"refActorBuffId":[1051],"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"サークル","type":1,"radius":6.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"thicc":4.0,"refActorRequireBuff":true,"refActorBuffId":[4165],"refActorUseBuffTime":true,"refActorBuffTimeMax":8.0,"refActorComparisonType":1,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":22.0,"Match":"(9708>40151)"},{"Type":2,"Duration":22.0,"Match":"(9708>40150)"}]}
~Lv2~{"Name":"P1_フェイトブレイカー：塔","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"1","type":1,"radius":4.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":4.0,"overlayText":"1","refActorNPCNameID":9707,"refActorRequireCast":true,"refActorCastId":[40131,40135],"refActorComparisonType":6,"tether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"2","type":1,"radius":4.0,"color":3355508223,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508223,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":4.0,"overlayText":"2","refActorNPCNameID":9707,"refActorRequireCast":true,"refActorCastId":[40122,40125],"refActorComparisonType":6,"tether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"3","type":1,"radius":4.0,"color":3357277952,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3357277952,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":4.0,"overlayText":"3","refActorNPCNameID":9707,"refActorRequireCast":true,"refActorCastId":[40123,40126],"refActorComparisonType":6,"tether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"4","type":1,"radius":4.0,"color":3372220160,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3372220160,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":4.0,"overlayText":"4","refActorNPCNameID":9707,"refActorRequireCast":true,"refActorCastId":[40124,40127],"refActorComparisonType":6,"tether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
~Lv2~{"Name":"P1_フェイトブレイカー：塔_爆印タンク立ち位置","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"Line1","type":2,"Enabled":false,"refX":94.0,"refY":94.0,"offX":106.0,"offY":94.0,"offZ":-1.9073486E-06,"radius":0.0,"color":3372166400,"Filled":false,"fillIntensity":0.345,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Line2","type":2,"Enabled":false,"refX":106.0,"refY":106.0,"refZ":-1.9073486E-06,"offX":94.0,"offY":106.0,"radius":0.0,"color":3372166400,"Filled":false,"fillIntensity":0.345,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Line3","type":2,"Enabled":false,"refX":96.0,"refY":92.5,"offX":96.0,"offY":107.5,"radius":0.0,"color":3372166400,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Line4","type":2,"Enabled":false,"refX":104.0,"refY":92.5,"refZ":1.9073486E-06,"offX":104.0,"offY":107.5,"radius":0.0,"color":3372166400,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Cir1","refX":105.65685,"refY":94.34315,"radius":0.5,"color":3356425984,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3369402112,"thicc":4.0,"overlayText":"Tank","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Cir2","refX":94.34315,"refY":94.34315,"refZ":1.9073486E-06,"radius":0.5,"color":3356425984,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3369402112,"thicc":4.0,"overlayText":"Tank","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":0.138},{"Name":"Cir3","refX":94.34315,"refY":105.65685,"radius":0.5,"color":3356425984,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3369402112,"thicc":4.0,"overlayText":"Tank","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Cir4","refX":105.65685,"refY":105.65685,"radius":0.5,"color":3356425984,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3369402112,"thicc":4.0,"overlayText":"Tank","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":2.0,"Match":"(9707>40130)","MatchDelay":9.2},{"Type":2,"Duration":3.3,"Match":"(9707>40134)","MatchDelay":7.9}]}
~Lv2~{"Name":"P1_幻影：サイクロニックブレイク_焔","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"ペア割り","type":1,"radius":5.5,"Donut":0.5,"color":4294967040,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4294967040,"thicc":5.0,"overlayText":"2 Stack","refActorComparisonType":1,"refActorType":1,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":7.0,"Match":"(9708>40329)","MatchDelay":2.0}]}
~Lv2~{"Name":"P1_幻影：サイクロニックブレイク_雷","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"散開(自分)","type":1,"radius":6.0,"color":4278779648,"Filled":false,"fillIntensity":0.39215687,"overlayBGColor":3355443200,"overlayTextColor":4278779648,"thicc":4.0,"overlayText":"Spread","refActorType":1,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"散開(その他)","type":1,"radius":6.0,"color":4278779648,"fillIntensity":0.11,"overlayBGColor":3355443200,"overlayTextColor":4278779648,"thicc":4.0,"overlayText":"Spread","refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":7.0,"Match":"(9708>40330)","MatchDelay":2.0}],"MaxDistance":7.5,"UseDistanceLimit":true,"DistanceLimitType":1}
~Lv2~{"Name":"P1_幻影： バーンストライク","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"雷","type":3,"refY":50.0,"radius":10.0,"color":4294918144,"Filled":false,"fillIntensity":0.3,"thicc":4.0,"refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40163],"refActorUseCastTime":true,"refActorCastTimeMax":9.4,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":2.0},{"Name":"雷(2.5秒前)","type":3,"refY":50.0,"radius":10.0,"color":4294918144,"fillIntensity":0.309,"thicc":0.0,"refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40163],"refActorUseCastTime":true,"refActorCastTimeMin":6.9,"refActorCastTimeMax":9.4,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"炎","type":3,"refY":50.0,"radius":5.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"thicc":4.0,"refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40161],"refActorUseCastTime":true,"refActorCastTimeMax":4.7,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":2.0},{"Name":"炎(2.5秒前)","type":3,"refY":50.0,"radius":5.0,"fillIntensity":0.304,"thicc":4.0,"refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40161],"refActorUseCastTime":true,"refActorCastTimeMin":5.2,"refActorCastTimeMax":7.7,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"炎(KB目安)","type":3,"refY":50.0,"radius":0.0,"fillIntensity":0.304,"thicc":4.0,"refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40161],"refActorUseCastTime":true,"refActorCastTimeMin":7.7,"refActorCastTimeMax":11.7,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"KB_手前_左","type":1,"offX":-0.5,"offY":14.5,"radius":0.4,"color":3355508589,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40161],"refActorUseCastTime":true,"refActorCastTimeMin":7.7,"refActorCastTimeMax":11.7,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"KB_手前_右","type":1,"offX":0.5,"offY":14.5,"radius":0.4,"color":3355508589,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40161],"refActorUseCastTime":true,"refActorCastTimeMin":7.7,"refActorCastTimeMax":11.7,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"KB_奥_左","type":1,"offX":-0.5,"offY":25.5,"radius":0.4,"color":3355508589,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40161],"refActorUseCastTime":true,"refActorCastTimeMin":7.7,"refActorCastTimeMax":11.7,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"KB_奥_右","type":1,"offX":0.5,"offY":25.5,"radius":0.4,"color":3355508589,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40161],"refActorUseCastTime":true,"refActorCastTimeMin":7.7,"refActorCastTimeMax":11.7,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
~Lv2~{"Enabled":false,"Name":"P1_光輪：光炎","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"焔_小","type":1,"radius":5.0,"color":4278255370,"fillIntensity":0.104,"thicc":4.0,"refActorNPCNameID":9710,"refActorRequireCast":true,"refActorCastId":[40152],"refActorComparisonType":6,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"焔_大","type":1,"radius":10.0,"color":4278190335,"fillIntensity":0.108,"thicc":4.0,"refActorNPCNameID":9710,"refActorRequireCast":true,"refActorCastId":[40153],"refActorComparisonType":6,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"雷_小","type":1,"radius":5.0,"color":4278255370,"fillIntensity":0.11,"thicc":4.0,"refActorNPCNameID":9711,"refActorRequireCast":true,"refActorCastId":[40152],"refActorComparisonType":6,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"雷_大","type":1,"radius":10.0,"color":4278190335,"fillIntensity":0.103,"thicc":4.0,"refActorNPCNameID":9711,"refActorRequireCast":true,"refActorCastId":[40153],"refActorComparisonType":6,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
~Lv2~{"Name":"P1_光輪：光炎(雷安置)","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"焔","type":1,"radius":10.0,"color":3355508719,"fillIntensity":0.31,"thicc":4.0,"refActorNPCNameID":9710,"refActorComparisonType":6,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"雷","type":1,"radius":5.0,"color":3355508558,"fillIntensity":0.0,"thicc":4.0,"refActorNPCNameID":9711,"refActorComparisonType":6,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":17.0,"Match":"(9708>40150)"}]}
~Lv2~{"Name":"P1_光輪：光炎(焔安置)","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"焔","type":1,"radius":5.0,"color":3356032768,"fillIntensity":0.0,"thicc":4.0,"refActorNPCNameID":9710,"refActorComparisonType":6,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"雷","type":1,"radius":10.0,"color":3355508719,"fillIntensity":0.31,"thicc":4.0,"refActorNPCNameID":9711,"refActorComparisonType":6,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":17.0,"Match":"(9708>40151)"}]}
~Lv2~{"Name":"P1_フェイトブレイカー：シンソイルセヴァー","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"西-対象","Enabled":false,"refX":95.0,"refY":100.0,"radius":0.5,"color":3372155125,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"西-北","Enabled":false,"refX":95.0,"refY":98.0,"radius":0.5,"color":3372155125,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"西-南","Enabled":false,"refX":95.0,"refY":102.0,"radius":0.5,"color":3372155125,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"西-外","Enabled":false,"refX":93.0,"refY":100.0,"radius":0.5,"color":3372155125,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"東-対象","Enabled":false,"refX":105.0,"refY":100.0,"radius":0.5,"color":3372155125,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"東-北","Enabled":false,"refX":105.0,"refY":98.0,"radius":0.5,"color":3372155125,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"東-南","Enabled":false,"refX":105.0,"refY":102.0,"radius":0.5,"color":3372155125,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"東-外","Enabled":false,"refX":107.0,"refY":100.0,"radius":0.5,"color":3372155125,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"テザー：炎(扇範囲)","type":4,"Enabled":false,"radius":5.0,"coneAngleMin":-45,"coneAngleMax":45,"color":3355481343,"Filled":false,"fillIntensity":0.146,"overlayBGColor":3355443200,"overlayTextColor":3355489279,"thicc":4.0,"refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"includeRotation":true,"FaceMe":true,"refActorTether":true,"refActorTetherTimeMin":7.0,"refActorTetherTimeMax":12.0,"refActorTetherParam2":249,"refActorTetherParam3":15,"refActorTetherConnectedWithPlayer":[]},{"Name":"テザー：炎(テキスト)","type":1,"Enabled":false,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3370581760,"thicc":0.0,"overlayText":"Stack","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorTether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":12.0,"refActorTetherParam2":249,"refActorTetherParam3":15,"refActorTetherConnectedWithPlayer":[]},{"Name":"テザー：雷(テキスト)","type":1,"Enabled":false,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508577,"thicc":0.0,"overlayText":"Spread","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorTether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":12.0,"refActorTetherParam2":287,"refActorTetherParam3":15,"refActorTetherConnectedWithPlayer":[]},{"Name":"浮遊警告","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":1.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"浮遊まで..","refActorType":1,"refActorTether":true,"refActorTetherTimeMin":4.0,"refActorTetherTimeMax":9.0,"refActorTetherParam3":15,"refActorTetherConnectedWithPlayer":[]},{"Name":"テザー：カウントダウン5","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508577,"overlayVOffset":2.1,"overlayFScale":3.0,"thicc":0.0,"overlayText":"5","refActorType":1,"refActorTether":true,"refActorTetherTimeMin":4.0,"refActorTetherTimeMax":5.0,"refActorTetherParam3":15,"refActorTetherConnectedWithPlayer":[]},{"Name":"テザー：カウントダウン4","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508577,"overlayVOffset":2.1,"overlayFScale":3.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTether":true,"refActorTetherTimeMin":5.0,"refActorTetherTimeMax":6.0,"refActorTetherParam3":15,"refActorTetherConnectedWithPlayer":[]},{"Name":"テザー：カウントダウン3","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508731,"overlayVOffset":2.1,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTether":true,"refActorTetherTimeMin":6.0,"refActorTetherTimeMax":7.0,"refActorTetherParam3":15,"refActorTetherConnectedWithPlayer":[]},{"Name":"テザー：カウントダウン2","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508731,"overlayVOffset":2.1,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTether":true,"refActorTetherTimeMin":7.0,"refActorTetherTimeMax":8.0,"refActorTetherParam3":15,"refActorTetherConnectedWithPlayer":[]},{"Name":"テザー：カウントダウン1","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":2.1,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"refActorTether":true,"refActorTetherTimeMin":8.0,"refActorTetherTimeMax":9.0,"refActorTetherParam3":15,"refActorTetherConnectedWithPlayer":[]},{"Name":"テザー：西側炎(頭割り位置)","type":1,"offX":-2.0,"radius":1.0,"Donut":0.2,"color":3372218624,"fillIntensity":0.0,"overlayBGColor":3355443200,"overlayTextColor":3372218624,"thicc":4.0,"overlayText":"Stack","refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"AdditionalRotation":3.1415927,"LimitDistance":true,"DistanceSourceX":95.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTether":true,"refActorTetherTimeMin":6.0,"refActorTetherTimeMax":12.0,"refActorTetherParam2":249,"refActorTetherParam3":15,"refActorTetherConnectedWithPlayer":[]},{"Name":"テザー：東側炎(頭割り位置)","type":1,"offX":2.0,"radius":1.0,"Donut":0.2,"color":3372218624,"fillIntensity":0.0,"overlayBGColor":3355443200,"overlayTextColor":3372218624,"thicc":4.0,"overlayText":"Stack","refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"LimitDistance":true,"DistanceSourceX":105.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTether":true,"refActorTetherTimeMin":6.0,"refActorTetherTimeMax":12.0,"refActorTetherParam2":249,"refActorTetherParam3":15,"refActorTetherConnectedWithPlayer":[]},{"Name":"テザー：両側雷(散開用直線南北)","type":3,"refY":-1.5,"offY":1.5,"radius":0.0,"color":3372169728,"fillIntensity":0.345,"thicc":4.0,"refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorTether":true,"refActorTetherTimeMin":6.0,"refActorTetherTimeMax":12.0,"refActorTetherParam2":287,"refActorTetherParam3":15,"refActorTetherConnectedWithPlayer":[]},{"Name":"テザー：両側雷(散開用北円)","type":1,"offY":-2.0,"radius":0.5,"color":3372169728,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorTether":true,"refActorTetherTimeMin":6.0,"refActorTetherTimeMax":12.0,"refActorTetherParam2":287,"refActorTetherParam3":15,"refActorTetherConnectedWithPlayer":[]},{"Name":"テザー：両側雷(散開用南円)","type":1,"offY":2.0,"radius":0.5,"color":3372169728,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorTether":true,"refActorTetherTimeMin":6.0,"refActorTetherTimeMax":12.0,"refActorTetherParam2":287,"refActorTetherParam3":15,"refActorTetherConnectedWithPlayer":[]},{"Name":"テザー：西側雷(散開用直線)","type":3,"refX":-1.5,"radius":0.0,"color":3372169728,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"LimitDistance":true,"DistanceSourceX":95.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTether":true,"refActorTetherTimeMin":6.0,"refActorTetherTimeMax":12.0,"refActorTetherParam2":287,"refActorTetherParam3":15,"refActorTetherConnectedWithPlayer":[]},{"Name":"テザー：西側雷(散開用円)","type":1,"offX":-2.0,"radius":0.5,"color":3372169728,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"LimitDistance":true,"DistanceSourceX":95.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTether":true,"refActorTetherTimeMin":6.0,"refActorTetherTimeMax":12.0,"refActorTetherParam2":287,"refActorTetherParam3":15,"refActorTetherConnectedWithPlayer":[]},{"Name":"テザー：東側雷(散開用直線)","type":3,"refX":1.5,"radius":0.0,"color":3372169728,"Filled":false,"fillIntensity":0.345,"thicc":4.0,"refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"LimitDistance":true,"DistanceSourceX":105.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTether":true,"refActorTetherTimeMin":6.0,"refActorTetherTimeMax":12.0,"refActorTetherParam2":287,"refActorTetherParam3":15,"refActorTetherConnectedWithPlayer":[]},{"Name":"テザー：東側雷(散開用円)","type":1,"offX":2.0,"radius":0.5,"color":3372169728,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"LimitDistance":true,"DistanceSourceX":105.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTether":true,"refActorTetherTimeMin":6.0,"refActorTetherTimeMax":12.0,"refActorTetherParam2":287,"refActorTetherParam3":15,"refActorTetherConnectedWithPlayer":[]},{"Name":"テザー：雷(扇範囲)","type":4,"radius":5.0,"coneAngleMin":-60,"coneAngleMax":60,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"includeRotation":true,"FaceMe":true,"refActorTether":true,"refActorTetherTimeMin":6.0,"refActorTetherTimeMax":12.0,"refActorTetherParam2":287,"refActorTetherParam3":15,"refActorTetherConnectedWithPlayer":[]},{"Name":"1回目散開","type":1,"offX":-2.5,"offY":-1.0,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508521,"overlayFScale":2.0,"thicc":0.0,"overlayText":"1.散開","refActorNPCNameID":9707,"refActorRequireCast":true,"refActorCastId":[40140],"refActorUseCastTime":true,"refActorCastTimeMax":12.7,"refActorUseOvercast":true,"refActorComparisonType":6,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"1回目頭割り","type":1,"offX":-2.5,"offY":-1.0,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3372220160,"overlayFScale":2.0,"thicc":0.0,"overlayText":"1.頭割り","refActorNPCNameID":9707,"refActorRequireCast":true,"refActorCastId":[40137],"refActorUseCastTime":true,"refActorCastTimeMax":12.7,"refActorUseOvercast":true,"refActorComparisonType":6,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"2回目散開","type":1,"offX":16.642,"offY":13.142,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508521,"overlayFScale":2.0,"thicc":0.0,"overlayText":"2.散開","refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40140],"refActorUseCastTime":true,"refActorCastTimeMax":12.7,"refActorUseOvercast":true,"refActorComparisonType":6,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":85.858,"DistanceSourceY":85.858,"DistanceMax":2.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"2回目頭割り","type":1,"offX":16.642,"offY":13.142,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3371433728,"overlayFScale":2.0,"thicc":0.0,"overlayText":"2.頭割り","refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40137],"refActorUseCastTime":true,"refActorCastTimeMax":12.7,"refActorUseOvercast":true,"refActorComparisonType":6,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":85.857864,"DistanceSourceY":85.857864,"DistanceMax":2.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"3回目散開","type":1,"offX":-2.5,"offY":21.0,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508521,"overlayFScale":2.0,"thicc":0.0,"overlayText":"3.散開","refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40140],"refActorUseCastTime":true,"refActorCastTimeMax":12.7,"refActorUseOvercast":true,"refActorComparisonType":6,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":80.0,"DistanceMax":2.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"3回目頭割り","type":1,"offX":-2.5,"offY":21.0,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3371433728,"overlayFScale":2.0,"thicc":0.0,"overlayText":"3.頭割り","refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40137],"refActorUseCastTime":true,"refActorCastTimeMax":12.7,"refActorUseOvercast":true,"refActorComparisonType":6,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":80.0,"DistanceMax":2.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"4回目散開","type":1,"offX":-11.642,"offY":15.183,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508521,"overlayFScale":2.0,"thicc":0.0,"overlayText":"4.散開","refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40140],"refActorUseCastTime":true,"refActorCastTimeMax":12.7,"refActorUseOvercast":true,"refActorComparisonType":6,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":114.142136,"DistanceSourceY":85.857864,"DistanceMax":2.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"4回目頭割り","type":1,"offX":-11.642,"offY":15.183,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3371433728,"overlayFScale":2.0,"thicc":0.0,"overlayText":"4.頭割り","refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40137],"refActorUseCastTime":true,"refActorCastTimeMax":12.7,"refActorUseOvercast":true,"refActorComparisonType":6,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":114.142,"DistanceSourceY":85.858,"DistanceMax":2.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"1回目散開：対象者(床+テザー)","type":1,"offX":-5.0,"radius":0.5,"color":3355508527,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9707,"refActorRequireCast":true,"refActorCastId":[40140],"refActorUseCastTime":true,"refActorCastTimeMax":12.7,"refActorUseOvercast":true,"refActorComparisonType":6,"onlyVisible":true,"tether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"1回目散開：北(床)","type":1,"offX":-5.0,"offY":-2.0,"radius":0.5,"color":3355508527,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9707,"refActorRequireCast":true,"refActorCastId":[40140],"refActorUseCastTime":true,"refActorCastTimeMax":12.7,"refActorUseOvercast":true,"refActorComparisonType":6,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"1回目散開：外(床)","type":1,"offX":-7.0,"radius":0.5,"color":3355508527,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9707,"refActorRequireCast":true,"refActorCastId":[40140],"refActorUseCastTime":true,"refActorCastTimeMax":12.7,"refActorUseOvercast":true,"refActorComparisonType":6,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"1回目散開：南(床)","type":1,"offX":-5.0,"offY":2.0,"radius":0.5,"color":3355508527,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9707,"refActorRequireCast":true,"refActorCastId":[40140],"refActorUseCastTime":true,"refActorCastTimeMax":12.7,"refActorUseOvercast":true,"refActorComparisonType":6,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"1回目頭割り：対象者(床+テザー)","type":1,"offX":-5.0,"radius":0.5,"color":4293721856,"Filled":false,"fillIntensity":0.39215687,"thicc":4.0,"refActorNPCNameID":9707,"refActorRequireCast":true,"refActorCastId":[40137],"refActorUseCastTime":true,"refActorCastTimeMax":12.7,"refActorUseOvercast":true,"refActorComparisonType":6,"onlyVisible":true,"tether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"1回目頭割り：外(床)","type":1,"offX":-7.0,"radius":0.5,"color":4293721856,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9707,"refActorRequireCast":true,"refActorCastId":[40137],"refActorUseCastTime":true,"refActorCastTimeMax":12.7,"refActorUseOvercast":true,"refActorComparisonType":6,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"2回目散開：北(床)","type":1,"offX":19.143,"offY":12.143,"radius":0.5,"color":3355508527,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40140],"refActorUseCastTime":true,"refActorCastTimeMax":12.7,"refActorUseOvercast":true,"refActorComparisonType":6,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":85.857864,"DistanceSourceY":85.857864,"DistanceMax":3.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"2回目散開：対象者(床+テザー)","type":1,"offX":19.143,"offY":14.143,"radius":0.5,"color":3355508527,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40140],"refActorUseCastTime":true,"refActorCastTimeMax":12.7,"refActorUseOvercast":true,"refActorComparisonType":6,"onlyVisible":true,"tether":true,"LimitDistance":true,"DistanceSourceX":85.857864,"DistanceSourceY":85.857864,"DistanceMax":3.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"2回目散開：外(床)","type":1,"offX":21.143,"offY":14.143,"radius":0.5,"color":3355508527,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40140],"refActorUseCastTime":true,"refActorCastTimeMax":12.7,"refActorUseOvercast":true,"refActorComparisonType":6,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":85.857864,"DistanceSourceY":85.857864,"DistanceMax":3.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"2回目散開：南(床)","type":1,"offX":19.143,"offY":16.143,"radius":0.5,"color":3355508527,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40140],"refActorUseCastTime":true,"refActorCastTimeMax":12.7,"refActorUseOvercast":true,"refActorComparisonType":6,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":85.857864,"DistanceSourceY":85.857864,"DistanceMax":3.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"2回目頭割り：対象者(床+テザー)","type":1,"offX":19.143,"offY":14.143,"radius":0.5,"color":4293721856,"Filled":false,"fillIntensity":0.39215687,"thicc":4.0,"refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40137],"refActorUseCastTime":true,"refActorCastTimeMax":12.7,"refActorUseOvercast":true,"refActorComparisonType":6,"onlyVisible":true,"tether":true,"LimitDistance":true,"DistanceSourceX":85.858,"DistanceSourceY":85.858,"DistanceMax":3.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"2回目頭割り：外(床)","type":1,"offX":21.143,"offY":14.143,"radius":0.5,"color":4293721856,"Filled":false,"fillIntensity":0.39215687,"thicc":4.0,"refActorNPCNameID":9708,"refActorRequireCast":true,"refActorCastId":[40137],"refActorUseCastTime":true,"refActorCastTimeMax":12.7,"refActorUseOvercast":true,"refActorComparisonType":6,"onlyVisible":true,"LimitDistance":true,"DistanceSourceX":85.858,"DistanceSourceY":85.858,"DistanceMax":3.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"浮遊拘束：十字1","type":3,"refX":-3.0,"offX":3.0,"radius":0.0,"fillIntensity":0.345,"thicc":4.0,"refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[1051,2304],"refActorRequireAllBuffs":true,"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"浮遊拘束：十字2","type":3,"refY":-3.0,"offY":3.0,"radius":0.0,"fillIntensity":0.345,"thicc":4.0,"refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[1051,2304],"refActorRequireAllBuffs":true,"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":21.7,"Match":"(9707>40140)"},{"Type":2,"Duration":21.7,"Match":"(9707>40137)"}]}
~Lv2~{"Name":"P1_フェイトブレイカー：シンソイルセヴァー(無職)","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"無職TANK","type":1,"radius":0.5,"color":3355503359,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3372220160,"overlayVOffset":2.0,"overlayFScale":1.5,"thicc":4.0,"overlayText":"TANK(無職)","refActorPlaceholder":["<t1>","<t2>"],"refActorRequireBuff":true,"refActorBuffId":[1051],"refActorRequireAllBuffs":true,"refActorRequireBuffsInvert":true,"refActorComparisonType":5,"tether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"無職HEALER","type":1,"radius":0.5,"color":3355503359,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508490,"overlayVOffset":2.0,"overlayFScale":1.5,"thicc":4.0,"overlayText":"HELAER(無職)","refActorPlaceholder":["<h1>","<h2>"],"refActorRequireBuff":true,"refActorBuffId":[1051],"refActorRequireAllBuffs":true,"refActorRequireBuffsInvert":true,"refActorComparisonType":5,"tether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"無職DPS","type":1,"radius":0.5,"color":3355503359,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":2.0,"overlayFScale":1.5,"thicc":4.0,"overlayText":"DPS(無職)","refActorPlaceholder":["<d1>","<d2>","<d3>","<d4>"],"refActorRequireBuff":true,"refActorBuffId":[1051],"refActorRequireAllBuffs":true,"refActorRequireBuffsInvert":true,"refActorComparisonType":5,"tether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"(9707>40140)","MatchDelay":8.7},{"Type":2,"TimeBegin":8.7,"Duration":5.0,"Match":"(9707>40137)","MatchDelay":8.7}]}
~Lv2~{"Name":"P2_シヴァ・ミトロン：DD_アイシクルインパクト","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"サークル","type":1,"radius":10.0,"color":4278190335,"fillIntensity":0.104,"thicc":4.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMin":4.0,"refActorCastTimeMax":10.0,"refActorComparisonType":6,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
~Lv2~{"Name":"P2_シヴァ・ミトロン：DD_アイスニードル","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"前後","type":3,"refY":40.0,"offY":-40.0,"radius":2.0,"color":4278190335,"fillIntensity":0.104,"thicc":4.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40200],"refActorUseCastTime":true,"refActorCastTimeMin":3.0,"refActorCastTimeMax":10.0,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":2.0},{"Name":"左右","type":3,"refY":40.0,"offY":-40.0,"radius":2.0,"color":4278190335,"fillIntensity":0.11,"thicc":4.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40200],"refActorUseCastTime":true,"refActorCastTimeMin":3.0,"refActorCastTimeMax":10.0,"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":1.5707964,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":2.0},{"Name":"右斜","type":3,"refY":40.0,"offY":-40.0,"radius":2.0,"color":4278190335,"fillIntensity":0.099,"thicc":4.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40200],"refActorUseCastTime":true,"refActorCastTimeMin":3.0,"refActorCastTimeMax":10.0,"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":0.7853982,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":2.0},{"Name":"左斜","type":3,"refY":40.0,"offY":-40.0,"radius":2.0,"color":4278190335,"fillIntensity":0.103,"thicc":4.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40200],"refActorUseCastTime":true,"refActorCastTimeMin":3.0,"refActorCastTimeMax":10.0,"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":5.497787,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":2.0}]}
~Lv2~{"Name":"P2_シヴァ・ミトロン：DD_ノックバックテザー","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"ノックバック","type":1,"radius":0.0,"color":4278255611,"Filled":false,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":6.0,"refActorNPCNameID":12809,"refActorComparisonType":6,"onlyVisible":true,"tether":true,"ExtraTetherLength":12.0,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.5,"Match":"(12809>40197)","MatchDelay":16.0}]}
~Lv2~{"Enabled":false,"Name":"P2_シヴァ・ミトロン：DD_ノックバック位置","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"ノックバック待機位置","type":1,"offY":14.0,"radius":1.0,"color":4278255395,"Filled":false,"fillIntensity":0.3,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":5.0,"overlayText":"ノックバック","refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":2.0,"Match":"(12809>40197)","MatchDelay":6.0}],"Freezing":true,"FreezeFor":14.0,"FreezeDisplayDelay":5.5}
~Lv2~{"Name":"P2_シヴァ・ミトロン：DD_設置カウントダウン","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"5","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/tag_ae5m_8s_0v.avfx","refActorVFXMin":2000,"refActorVFXMax":3000,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"4","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"4","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/tag_ae5m_8s_0v.avfx","refActorVFXMin":3000,"refActorVFXMax":4000,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"3","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"3","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/tag_ae5m_8s_0v.avfx","refActorVFXMin":4000,"refActorVFXMax":5000,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"2","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"2","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/tag_ae5m_8s_0v.avfx","refActorVFXMin":5000,"refActorVFXMax":6000,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"1","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"1","refActorComparisonType":7,"refActorVFXPath":"vfx/lockon/eff/tag_ae5m_8s_0v.avfx","refActorVFXMin":6000,"refActorVFXMax":7000,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":20.0,"Match":"(12809>40197)"}]}
~Lv2~{"Name":"P2：シヴァ・ミトロン：DD_扇担当位置","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"誘導位置_内1","type":1,"offY":15.0,"radius":0.2,"color":4278255395,"Filled":false,"fillIntensity":0.3,"thicc":5.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"誘導位置_内2","type":1,"offX":1.0,"offY":16.0,"radius":0.2,"color":4278255395,"Filled":false,"fillIntensity":0.3,"thicc":5.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"誘導位置_外1","type":1,"offY":-0.2,"radius":0.2,"color":4278255395,"Filled":false,"fillIntensity":0.3,"thicc":5.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"誘導位置_外2","type":1,"offX":16.2,"offY":16.0,"radius":0.2,"color":4278255395,"Filled":false,"fillIntensity":0.3,"thicc":5.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":2.0,"Match":"(12809>40197)","MatchDelay":6.0}],"Freezing":true,"FreezeFor":6.0}
~Lv2~{"Name":"P2_シヴァ・ミトロン：DD_AoE担当位置","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"待機位置_内1","type":1,"offX":-2.7,"offY":13.3,"radius":0.2,"color":4278255395,"Filled":false,"fillIntensity":0.3,"thicc":5.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"待機位置_内2","type":1,"offX":2.7,"offY":13.3,"radius":0.2,"color":4278255395,"Filled":false,"fillIntensity":0.3,"thicc":5.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"待機位置_外1","type":1,"offX":13.76,"offY":2.2,"radius":0.2,"color":4278255395,"Filled":false,"fillIntensity":0.3,"thicc":5.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"待機位置_外2","type":1,"offX":-13.76,"offY":2.2,"radius":0.2,"color":4278255395,"Filled":false,"fillIntensity":0.3,"thicc":5.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40198],"refActorUseCastTime":true,"refActorCastTimeMax":1.0,"refActorComparisonType":6,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":2.0,"Match":"(12809>40197)","MatchDelay":6.0}],"Freezing":true,"FreezeFor":6.0}
~Lv2~{"Enabled":false,"Name":"P2_シヴァ・ミトロン：DD_視線","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"テザー","type":1,"radius":0.0,"color":3372024063,"Filled":false,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4278190335,"overlayVOffset":4.0,"overlayFScale":4.0,"thicc":5.0,"overlayText":"<●>","refActorNPCNameID":12809,"refActorComparisonType":6,"onlyVisible":true,"tether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"目線","type":4,"Enabled":false,"radius":1.0,"coneAngleMin":-45,"coneAngleMax":45,"color":4278190335,"fillIntensity":1.0,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"overlayVOffset":2.0,"overlayFScale":2.0,"overlayText":"視線","refActorType":1,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"(12809>40197)","MatchDelay":28.0}]}
~Lv2~{"Name":"P2_巫女の鏡像：DD_回転方向+移動方向の参考ライン","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"巫女の鏡像","type":3,"refY":30.0,"offY":-10.0,"radius":0.0,"color":3355508731,"fillIntensity":0.345,"thicc":8.0,"refActorNPCNameID":13554,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"背面安置目安","type":4,"radius":11.75,"coneAngleMin":135,"coneAngleMax":225,"color":3371826944,"Filled":false,"fillIntensity":0.5,"thicc":8.0,"refActorNPCNameID":13554,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"頭割り","type":1,"radius":5.8,"Donut":0.2,"color":3372212224,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3372212224,"thicc":4.0,"overlayText":"Stack","refActorPlaceholder":["<h1>","<h2>"],"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"巫女足元組(ダッシュ)90","type":1,"offY":-8.0,"radius":1.8,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"thicc":4.0,"overlayText":"氷着弾後\\nGunDash","refActorNPCNameID":13554,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"巫女足元組(通常)135","type":1,"offX":-12.727922,"offY":-2.727922,"radius":1.8,"color":3355508484,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508533,"thicc":4.0,"overlayText":"頭割り後\\n   Dash","refActorNPCNameID":13554,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"巫女足元組(ゆっくり)180","type":1,"offX":-18.0,"offY":10.0,"radius":1.8,"color":3370188544,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3370188544,"thicc":4.0,"overlayText":"頭割り後\\n   Slow","refActorNPCNameID":13554,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"巫女足元組(反時計)225","type":1,"offX":-12.727922,"offY":22.727922,"radius":1.8,"color":3355508719,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508719,"thicc":4.0,"overlayText":"反時計\\n Dash","refActorNPCNameID":13554,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"巫女対角組(ダッシュ)270","type":1,"offY":28.0,"radius":1.8,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"thicc":4.0,"overlayText":"氷着弾後\\nGunDash","refActorNPCNameID":13554,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"巫女対角組(通常)315","type":1,"offX":12.727922,"offY":22.727922,"radius":1.8,"color":3355508484,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508533,"thicc":4.0,"overlayText":"頭割り後\\n   Dash","refActorNPCNameID":13554,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"巫女対角組(ゆっくり)0","type":1,"offX":18.0,"offY":10.0,"radius":1.8,"color":3370188544,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3370188544,"thicc":4.0,"overlayText":"頭割り後\\n   Slow","refActorNPCNameID":13554,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"巫女対角組(反時計)45","type":1,"offX":12.727922,"offY":-2.727922,"radius":1.8,"color":3355508719,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508719,"thicc":4.0,"overlayText":"反時計\\n Dash","refActorNPCNameID":13554,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"巫女足元組(ダッシュ)90：矢印","type":3,"refY":-8.0,"offX":-4.1411047,"offY":-5.454813,"radius":0.0,"fillIntensity":0.345,"thicc":4.0,"refActorNPCNameID":13554,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"巫女足元組(通常)135：矢印","type":3,"refX":-12.728,"refY":-2.728,"offX":-13.856406,"offY":2.0,"radius":0.0,"color":3355508577,"Filled":false,"fillIntensity":0.345,"thicc":4.0,"refActorNPCNameID":13554,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"巫女足元組(ゆっくり)180：矢印","type":3,"refX":-18.0,"refY":10.0,"offX":-17.386665,"offY":14.658743,"radius":0.0,"color":3370188544,"fillIntensity":0.345,"thicc":4.0,"refActorNPCNameID":13554,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"巫女足元組(反時計)225：矢印","type":3,"refX":-12.728,"refY":22.728,"offX":-13.856406,"offY":18.0,"radius":0.0,"color":3355508719,"fillIntensity":0.345,"thicc":4.0,"refActorNPCNameID":13554,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"巫女対角組(ダッシュ)270：矢印","type":3,"refY":28.0,"offX":4.1411047,"offY":25.454813,"radius":0.0,"fillIntensity":0.345,"thicc":4.0,"refActorNPCNameID":13554,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"巫女対角組(通常)315：矢印","type":3,"refX":12.728,"refY":22.728,"offX":13.856406,"offY":18.0,"radius":0.0,"color":3355508577,"thicc":4.0,"refActorNPCNameID":13554,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"巫女対角組(ゆっくり)0：矢印","type":3,"refX":18.0,"refY":10.0,"offX":17.386665,"offY":5.341257,"radius":0.0,"color":3370188544,"fillIntensity":0.345,"thicc":4.0,"refActorNPCNameID":13554,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"巫女対角組(反時計)45：矢印","type":3,"refX":12.728,"refY":-2.728,"offX":13.856406,"offY":2.0,"radius":0.0,"color":3355508719,"fillIntensity":0.345,"thicc":4.0,"refActorNPCNameID":13554,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":13.0,"Match":"(12809>40197)","MatchDelay":20.7}]}
~Lv2~{"Name":"P2_巫女の鏡像：DD_アクス/サイスキック","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"アクスキック","type":1,"radius":16.0,"fillIntensity":0.309,"thicc":8.0,"refActorNPCNameID":13554,"refActorRequireCast":true,"refActorCastId":[40202],"refActorComparisonType":6,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"サイスキック","type":1,"radius":4.0,"Donut":16.0,"fillIntensity":0.305,"thicc":8.0,"refActorNPCNameID":13554,"refActorRequireCast":true,"refActorCastId":[40203],"refActorComparisonType":6,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
~Lv2~{"Name":"P2_巫女の鏡像：DD_双剣技","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"閑寂の双剣技_1段目","type":4,"radius":40.0,"coneAngleMin":-45,"coneAngleMax":45,"color":4278190335,"fillIntensity":0.11,"refActorNPCNameID":13554,"refActorRequireCast":true,"refActorCastId":[40194],"refActorUseCastTime":true,"refActorCastTimeMax":3.0,"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":3.1415927,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"閑寂の双剣技_2段目","type":4,"radius":40.0,"coneAngleMin":-135,"coneAngleMax":135,"color":4278190335,"fillIntensity":0.104,"refActorNPCNameID":13554,"refActorRequireCast":true,"refActorCastId":[40194],"refActorUseCastTime":true,"refActorCastTimeMin":3.0,"refActorCastTimeMax":6.0,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"静寂の双剣技_1段目","type":4,"radius":40.0,"coneAngleMin":-135,"coneAngleMax":135,"color":4278190335,"fillIntensity":0.104,"refActorNPCNameID":13554,"refActorRequireCast":true,"refActorCastId":[40193],"refActorUseCastTime":true,"refActorCastTimeMax":3.0,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"静寂の双剣技_2段目","type":4,"radius":40.0,"coneAngleMin":135,"coneAngleMax":225,"color":4278190335,"fillIntensity":0.104,"refActorNPCNameID":13554,"refActorRequireCast":true,"refActorCastId":[40193],"refActorUseCastTime":true,"refActorCastTimeMin":3.0,"refActorCastTimeMax":6.0,"refActorUseOvercast":true,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
~Lv2~{"Enabled":false,"Name":"P2_DD&光の暴走：沼","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"沼","type":1,"radius":6.0,"color":3372154903,"fillIntensity":0.0,"thicc":4.0,"refActorNPCID":2014287,"refActorComparisonType":4,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
~Lv2~{"Name":"P2_鏡像&シヴァ・ミトロン：DD_視線&氷床警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"警告メッセージ","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"視線&氷床まで","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"MatchIntl":{"Jp":"光によりて、静寂を！"}}]}
~Lv2~{"Name":"P2_鏡像&シヴァ・ミトロン：DD_視線&氷床_CD5","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508595,"overlayVOffset":3.1,"overlayFScale":4.0,"thicc":0.0,"overlayText":"5","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"Jp":"光によりて、静寂を！"}}]}
~Lv2~{"Name":"P2_鏡像&シヴァ・ミトロン：DD_視線&氷床_CD4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508595,"overlayVOffset":3.1,"overlayFScale":4.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"Jp":"光によりて、静寂を！"},"MatchDelay":1.0}]}
~Lv2~{"Name":"P2_鏡像&シヴァ・ミトロン：DD_視線&氷床_CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508731,"overlayVOffset":3.1,"overlayFScale":4.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"Jp":"光によりて、静寂を！"},"MatchDelay":2.0}]}
~Lv2~{"Name":"P2_鏡像&シヴァ・ミトロン：DD_視線&氷床_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508731,"overlayVOffset":3.1,"overlayFScale":4.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"Jp":"光によりて、静寂を！"},"MatchDelay":3.0}]}
~Lv2~{"Name":"P2_鏡像&シヴァ・ミトロン：DD_視線&氷床_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.1,"overlayFScale":4.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"Jp":"光によりて、静寂を！"},"MatchDelay":4.0}]}
~Lv2~{"Name":"P2_シヴァ・ミトロン：サイスキック","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"サイスキック","type":1,"radius":4.0,"Donut":16.0,"fillIntensity":0.099,"thicc":4.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40203],"refActorComparisonType":6,"onlyTargetable":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
~Lv2~{"Name":"P2_シヴァ・ミトロン：サイスキック(近接扇)","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"サイス扇","type":4,"radius":4.0,"coneAngleMin":-15,"coneAngleMax":15,"fillIntensity":0.119,"thicc":4.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40203],"refActorComparisonType":6,"includeRotation":true,"onlyTargetable":true,"onlyVisible":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
~Lv2~{"Name":"P2_鏡：ミラーリング・サイスキック","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"氷面鏡：安置","type":1,"radius":4.0,"color":3371433728,"Filled":false,"fillIntensity":0.228,"thicc":8.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"onlyUnTargetable":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(東)：散開1","type":1,"offX":-0.796,"offY":-3.386,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":120.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(東)：散開2","type":1,"offX":-2.63,"offY":-2.133,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":120.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(東)：散開3","type":1,"offX":-3.5,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":120.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(東)：散開4","type":1,"offX":-2.63,"offY":2.133,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":120.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(東)：散開5","type":1,"offX":-0.796,"offY":3.386,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":120.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"東：散開1(確認用)","Enabled":false,"refX":120.0,"refY":100.0,"offX":-0.7962488,"offY":-3.3861394,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"東：散開2(確認用)","Enabled":false,"refX":120.0,"refY":100.0,"offX":-2.6304424,"offY":-2.1327136,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"東：散開3(確認用)","Enabled":false,"refX":120.0,"refY":100.0,"offX":-3.5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"東：散開4(確認用)","Enabled":false,"refX":120.0,"refY":100.0,"offX":-2.6304424,"offY":2.1327136,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"東：散開5(確認用)","Enabled":false,"refX":120.0,"refY":100.0,"offX":-0.7962488,"offY":3.3861394,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(北東)：散開1","type":1,"offX":-2.957,"offY":-1.831,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":114.142,"DistanceSourceY":85.858,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(北東)：散開2","type":1,"offX":-3.368,"offY":0.352,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":114.142,"DistanceSourceY":85.858,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(北東)：散開3","type":1,"offX":-2.475,"offY":2.475,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":114.142,"DistanceSourceY":85.858,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(北東)：散開4","type":1,"offX":-0.352,"offY":3.368,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":114.142,"DistanceSourceY":85.858,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(北東)：散開5","type":1,"offX":1.831,"offY":2.957,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":114.142,"DistanceSourceY":85.858,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北東：散開1(確認用)","Enabled":false,"refX":114.142136,"refY":85.857864,"offX":-2.9572594,"offY":-1.8314649,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北東：散開2(確認用)","Enabled":false,"refX":114.142136,"refY":85.857864,"offX":-3.3679242,"offY":0.3518118,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北東：散開3(確認用)","Enabled":false,"refX":114.142136,"refY":85.857864,"offX":-2.4747381,"offY":2.4747381,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北東：散開4(確認用)","Enabled":false,"refX":114.142136,"refY":85.857864,"offX":-0.3518118,"offY":3.3679242,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北東：散開5(確認用)","Enabled":false,"refX":114.142136,"refY":85.857864,"offX":1.8314649,"offY":2.9572594,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(北)：散開1","type":1,"offX":-3.386,"offY":0.796,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":80.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(北)：散開2","type":1,"offX":-2.133,"offY":2.63,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":80.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(北)：散開3","type":1,"offY":3.5,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":80.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(北)：散開4","type":1,"offX":2.133,"offY":2.63,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":80.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(北)：散開5","type":1,"offX":3.386,"offY":0.796,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":80.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北：散開1(確認用)","Enabled":false,"refX":100.0,"refY":80.0,"offX":-3.3861394,"offY":0.7962488,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北：散開2(確認用)","Enabled":false,"refX":100.0,"refY":80.0,"offX":-2.1327136,"offY":2.6304424,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北：散開3(確認用)","Enabled":false,"refX":100.0,"refY":80.0,"offY":3.5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北：散開4(確認用)","Enabled":false,"refX":100.0,"refY":80.0,"offX":2.1327136,"offY":2.6304424,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北：散開5(確認用)","Enabled":false,"refX":100.0,"refY":80.0,"offX":3.3861394,"offY":0.7962488,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(北西)：散開1","type":1,"offX":-1.871,"offY":2.917,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":85.898,"DistanceSourceY":85.898,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(北西)：散開2","type":1,"offX":0.312,"offY":3.328,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":85.898,"DistanceSourceY":85.898,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(北西)：散開3","type":1,"offX":2.435,"offY":2.435,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":85.898,"DistanceSourceY":85.898,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(北西)：散開4","type":1,"offX":3.328,"offY":0.312,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":85.898,"DistanceSourceY":85.898,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(北西)：散開5","type":1,"offX":2.917,"offY":-1.871,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":85.898,"DistanceSourceY":85.898,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北西：散開1(確認用)","Enabled":false,"refX":85.898,"refY":85.857864,"offX":-1.8714648,"offY":2.9172595,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北西：散開2(確認用)","Enabled":false,"refX":85.857864,"refY":85.857864,"offX":0.3118118,"offY":3.3279243,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北西：散開3(確認用)","Enabled":false,"refX":85.857864,"refY":85.857864,"offX":2.4347382,"offY":2.4347382,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北西：散開4(確認用)","Enabled":false,"refX":85.857864,"refY":85.857864,"offX":3.3279243,"offY":0.3118118,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北西：散開5(確認用)","Enabled":false,"refX":85.857864,"refY":85.857864,"offX":2.9172595,"offY":-1.8714648,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(西)：散開1","type":1,"offX":0.796,"offY":3.386,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":80.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(西)：散開2","type":1,"offX":2.63,"offY":2.133,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":80.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(西)：散開3","type":1,"offX":3.5,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":80.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(西)：散開4","type":1,"offX":2.63,"offY":-2.133,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":80.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(西)：散開5","type":1,"offX":0.796,"offY":-3.386,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":80.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"西：散開1(確認用)","Enabled":false,"refX":80.0,"refY":100.0,"offX":0.7962488,"offY":3.3861394,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"西：散開2(確認用)","Enabled":false,"refX":80.0,"refY":100.0,"offX":2.6304424,"offY":2.1327136,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"西：散開3(確認用)","Enabled":false,"refX":80.0,"refY":100.0,"offX":3.5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"西：散開4(確認用)","Enabled":false,"refX":80.0,"refY":100.0,"offX":2.6304424,"offY":-2.1327136,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"西：散開5(確認用)","Enabled":false,"refX":80.0,"refY":100.0,"offX":0.7962488,"offY":-3.3861394,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(南西)：散開1","type":1,"offX":2.9572594,"offY":1.8314649,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":85.857864,"DistanceSourceY":114.142136,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(南西)：散開2","type":1,"offX":3.3679242,"offY":-0.35181183,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":85.857864,"DistanceSourceY":114.142136,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(南西)：散開3","type":1,"offX":2.4747381,"offY":-2.4747381,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":85.857864,"DistanceSourceY":114.142136,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(南西)：散開4","type":1,"offX":0.35181183,"offY":-3.3679242,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":85.857864,"DistanceSourceY":114.142136,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(南西)：散開5","type":1,"offX":-1.8314649,"offY":-2.9572594,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":85.857864,"DistanceSourceY":114.142136,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(南)：散開1","type":1,"offX":3.386,"offY":-0.796,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":120.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(南)：散開2","type":1,"offX":2.133,"offY":-2.63,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":120.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(南)：散開3","type":1,"offY":-3.5,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":120.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(南)：散開4","type":1,"offX":-2.133,"offY":-2.63,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":120.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(南)：散開5","type":1,"offX":-3.386,"offY":-0.796,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":120.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南：散開1(確認用)","Enabled":false,"refX":100.0,"refY":120.0,"offX":3.3861394,"offY":-0.7962488,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南：散開2(確認用)","Enabled":false,"refX":100.0,"refY":120.0,"offX":2.1327136,"offY":-2.6304424,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南：散開3(確認用)","Enabled":false,"refX":100.0,"refY":120.0,"offY":-3.5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南：散開4(確認用)","Enabled":false,"refX":100.0,"refY":120.0,"offX":-2.133,"offY":-2.6304424,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南：散開5(確認用)","Enabled":false,"refX":100.0,"refY":120.0,"offX":-3.3861394,"offY":-0.7962488,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(南東)：散開1","type":1,"offX":1.8314649,"offY":-2.9572594,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":114.142,"DistanceSourceY":114.142,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(南東)：散開2","type":1,"offX":-0.35181183,"offY":-3.3679242,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":114.142,"DistanceSourceY":114.142,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(南東)：散開3","type":1,"offX":-2.4747381,"offY":-2.4747381,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":114.142,"DistanceSourceY":114.142,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(南東)：散開4","type":1,"offX":-3.3679242,"offY":-0.3518118,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":114.142,"DistanceSourceY":114.142,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(南東)：散開5","type":1,"offX":-2.9572594,"offY":1.8314649,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":114.142,"DistanceSourceY":114.142,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南東(-45)","Enabled":false,"refX":114.142136,"refY":114.142136,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南東散開1(19.5 ,  -45+10)","Enabled":false,"refX":115.973465,"refY":111.18474,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南東散開2(17.5 ,  -45+7)","Enabled":false,"refX":110.77408,"refY":113.79019,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南東散開3(16.5 ,  -45)","Enabled":false,"refX":111.66726,"refY":111.66726,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南東散開4(17.5 ,  -45-7)","Enabled":false,"refX":113.79019,"refY":110.77408,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南東散開5(19.5, -45-10)","Enabled":false,"refX":111.18474,"refY":115.973465,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"円周","Enabled":false,"refX":100.0,"refY":100.0,"radius":16.5,"Filled":false,"fillIntensity":0.5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡：(東)：候補潰し1","type":1,"offX":-17.867287,"offY":-17.369558,"radius":0.5,"color":4278190335,"fillIntensity":1.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":120.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡：(東)：候補潰し2","type":1,"offX":-17.867287,"offY":17.369556,"radius":0.5,"color":4278190335,"fillIntensity":1.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":120.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡：(北東)：候補潰し1","type":1,"offX":-24.916,"offY":0.352,"radius":0.5,"color":4278190335,"fillIntensity":1.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":114.142,"DistanceSourceY":85.858,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡：(北東)：候補潰し2","type":1,"offX":-0.352,"offY":24.916,"radius":0.5,"color":4278190335,"fillIntensity":1.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":114.142,"DistanceSourceY":85.858,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡：(北)：候補潰し1","type":1,"offX":-17.369558,"offY":17.867287,"radius":0.5,"color":4278190335,"fillIntensity":1.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":80.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡：(北)：候補潰し2","type":1,"offX":17.369558,"offY":17.867287,"radius":0.5,"color":4278190335,"fillIntensity":1.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":80.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡：(北西)：候補潰し1","type":1,"offX":0.35181183,"offY":24.916077,"radius":0.5,"color":4278190335,"fillIntensity":1.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":85.857864,"DistanceSourceY":85.857864,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡：(北西)：候補潰し2","type":1,"offX":24.916077,"offY":0.3518118,"radius":0.5,"color":4278190335,"fillIntensity":1.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":85.857864,"DistanceSourceY":85.857864,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡：(西)：候補潰し1","type":1,"offX":17.867,"offY":17.37,"radius":0.5,"color":4278190335,"fillIntensity":1.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":80.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡：(西)：候補潰し2","type":1,"offX":17.867287,"offY":-17.369558,"radius":0.5,"color":4278190335,"fillIntensity":1.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":80.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡：(南西)：候補潰し1","type":1,"offX":24.916077,"offY":-0.35181183,"radius":0.5,"color":4278190335,"fillIntensity":1.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":85.858,"DistanceSourceY":114.142,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡：(南西)：候補潰し2","type":1,"offX":0.35181183,"offY":-24.916077,"radius":0.5,"color":4278190335,"fillIntensity":1.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":85.858,"DistanceSourceY":114.142,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(南)：候補潰し1","type":1,"offX":17.369558,"offY":-17.867287,"radius":0.5,"color":4278190335,"fillIntensity":1.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":120.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(南)：候補潰し2","type":1,"offX":-17.369558,"offY":-17.867287,"radius":0.5,"color":4278190335,"fillIntensity":1.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":120.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(南東)：候補潰し1 +83","type":1,"offX":-0.35181183,"offY":-24.916077,"radius":0.5,"color":4278190335,"fillIntensity":1.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":114.142,"DistanceSourceY":114.142,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤鏡(南東)：候補潰し2 -83","type":1,"offX":-24.916077,"offY":-0.35181183,"radius":0.5,"color":4278190335,"fillIntensity":1.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[19855,19898,19910,19963,19969,19975,40204,40205],"refActorComparisonType":6,"LimitDistance":true,"DistanceSourceX":114.142,"DistanceSourceY":114.142,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"確認用","Enabled":false,"refX":120.0,"refY":100.0,"offX":-17.867287,"offY":-17.369558,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
~Lv2~{"Name":"P2_鏡：サイスキック","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"サイスキック","type":1,"radius":4.0,"Donut":16.0,"fillIntensity":0.3,"thicc":4.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[40205],"refActorUseCastTime":true,"refActorCastTimeMax":10.0,"refActorUseOvercast":true,"refActorComparisonType":6,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"前方扇","type":4,"radius":40.0,"coneAngleMin":-15,"coneAngleMax":15,"color":4278190335,"fillIntensity":0.3,"thicc":3.0,"refActorNPCNameID":9317,"refActorRequireCast":true,"refActorCastId":[40205],"refActorComparisonType":6,"includeHitbox":true,"includeRotation":true,"FaceMe":true,"DistanceMax":13.2,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":4.0}],"MaxDistance":15.0,"UseDistanceLimit":true,"DistanceLimitType":1}
~Lv2~{"Name":"P2_シヴァ・ミトロン：バニシュガ_散開","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"サークル","type":1,"radius":5.0,"color":4278255395,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278255395,"thicc":4.0,"overlayText":"Spread","refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"テキスト","type":1,"Enabled":false,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"散開","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":6.0,"Match":"(12809>40221)"}],"MaxDistance":10.0,"UseDistanceLimit":true,"DistanceLimitType":1}
~Lv2~{"Name":"P2_シヴァ・ミトロン：バニシュガ_ペア","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"ペア割","type":1,"radius":4.8,"Donut":0.2,"color":4294967040,"Filled":false,"fillIntensity":0.3,"overlayBGColor":4278190080,"overlayTextColor":4294967040,"thicc":5.0,"overlayText":"2Stack","refActorPlaceholder":["<t1>","<t2>","<h1>","<h2>"],"refActorComparisonType":5,"refActorType":1,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":6.0,"Match":"(12809>40220)"}]}
~Lv2~{"Name":"P2_シヴァ・ミトロン：光の暴走_事前整列","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"22.5","refX":107.39104,"refY":96.93853,"radius":1.0,"color":3355508527,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"67.5","refX":103.06147,"refY":92.60896,"radius":1.0,"color":3355508527,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"112.5","refX":96.93853,"refY":92.60896,"radius":1.0,"color":3355508527,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"157.5","refX":92.60896,"refY":96.93853,"radius":1.0,"color":3355508527,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"202.5","refX":92.60896,"refY":103.06147,"radius":1.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"247.5","refX":96.93853,"refY":107.39104,"radius":1.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"292.5","refX":103.06147,"refY":107.39104,"radius":1.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"337.5","refX":107.39104,"refY":103.06147,"radius":1.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"","Enabled":false,"refX":100.0,"refY":100.0,"radius":8.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"TimeBegin":3.0,"Duration":9.0,"Match":"(12809>40179)","MatchDelay":33.0}]}
~Lv2~{"Name":"P2_シヴァ・ミトロン：光の暴走","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"テキスト","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"塔担当","refActorRequireBuff":true,"refActorBuffId":[2257],"refActorUseBuffParam":true,"refActorBuffParam":2,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":8.0,"Match":"(12809>40212)","MatchDelay":23.0}]}
~Lv2~{"Name":"P2_シヴァ・ミトロン：AoE捨てルート(南北スタート)","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"北1","refX":100.0,"refY":96.5,"radius":0.3,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北1-2","type":2,"refX":100.24933,"refY":96.29439,"refZ":1.9073486E-06,"offX":105.598015,"offY":94.180824,"radius":0.0,"color":3355508558,"fillIntensity":0.345,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北2","refX":105.9045,"refY":94.10553,"refZ":-1.9073486E-06,"radius":0.3,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北2-3","type":2,"refX":106.05868,"refY":94.38453,"offX":108.3891,"offY":99.71562,"radius":0.0,"color":3355508558,"fillIntensity":0.345,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北3","refX":108.5,"refY":100.0,"refZ":-1.9073486E-06,"radius":0.3,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北3-4","type":2,"refX":108.47822,"refY":100.28962,"offX":106.192604,"offY":105.81142,"offZ":3.8146973E-06,"radius":0.0,"color":3355508558,"fillIntensity":0.345,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北4","refX":106.0,"refY":106.0,"refZ":3.8146973E-06,"radius":0.3,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北4-5","type":2,"refX":105.88873,"refY":106.22227,"refZ":3.8146973E-06,"offX":102.022354,"offY":110.855446,"offZ":3.8146973E-06,"radius":0.0,"color":3355508558,"fillIntensity":0.345,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北5","refX":101.91996,"refY":110.987404,"radius":0.3,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南1","refX":100.0,"refY":103.5,"radius":0.3,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南1-2","type":2,"refX":99.72942,"refY":103.65207,"refZ":1.9073486E-06,"offX":94.37011,"offY":105.85496,"radius":0.0,"color":3355508558,"fillIntensity":0.345,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南2","refX":94.08508,"refY":105.93518,"refZ":-1.9073486E-06,"radius":0.3,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南2-3","type":2,"refX":93.93385,"refY":105.6341,"refZ":-1.9073486E-06,"offX":91.52345,"offY":100.3072,"offZ":1.9073486E-06,"radius":0.0,"color":3355508558,"fillIntensity":0.345,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南3","refX":91.54346,"refY":100.02777,"refZ":1.9073486E-06,"radius":0.3,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南3-4","type":2,"refX":91.46848,"refY":99.688934,"refZ":3.8146973E-06,"offX":93.82222,"offY":94.23363,"radius":0.0,"color":3355508558,"fillIntensity":0.345,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南4","refX":94.027084,"refY":93.97765,"refZ":-1.9073486E-06,"radius":0.3,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南4-5","type":2,"refX":94.17444,"refY":93.726776,"refZ":1.9073486E-06,"offX":98.047356,"offY":89.23599,"offZ":5.722046E-06,"radius":0.0,"color":3355508558,"fillIntensity":0.345,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南5","refX":98.26315,"refY":89.08989,"refZ":1.9073486E-06,"radius":0.3,"color":3355508558,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":15.3,"Match":"(12809>40212)","MatchDelay":4.7}]}
~Lv2~{"Enabled":false,"Name":"P2_シヴァ・ミトロン：AoE捨てルート(リリド)","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"南1","refX":97.17157,"refY":102.82843,"radius":0.3,"color":3355508484,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南1-2","type":2,"refX":96.95679,"refY":103.04511,"refZ":1.9073486E-06,"offX":92.484085,"offY":107.56158,"offZ":1.9073486E-06,"radius":0.0,"color":3355508484,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南2","refX":92.221825,"refY":107.778175,"radius":0.3,"color":3355508484,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南2-3","type":2,"refX":92.050385,"refY":107.543884,"offX":89.082504,"offY":101.81292,"radius":0.0,"color":3355508484,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南3","refX":89.0,"refY":101.5,"refZ":-1.9073486E-06,"radius":0.3,"color":3355508484,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南3-4","type":2,"refX":89.05497,"refY":101.21996,"refZ":1.9073486E-06,"offX":92.79052,"offY":96.27556,"offZ":-1.9073486E-06,"radius":0.0,"color":3355508484,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南4","refX":93.0,"refY":96.0,"refZ":-1.9073486E-06,"radius":0.3,"color":3355508484,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南4-5","type":2,"refX":93.130295,"refY":95.71388,"refZ":1.9073486E-06,"offX":96.63485,"offY":90.75957,"radius":0.0,"color":3355508484,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南5","refX":96.8,"refY":90.5,"radius":0.3,"color":3355508484,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北1","refX":102.82843,"refY":97.17157,"radius":0.3,"color":3355508484,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北1-2","type":2,"refX":103.05807,"refY":96.96979,"refZ":-3.8146973E-06,"offX":107.58699,"offY":92.41748,"offZ":3.8146973E-06,"radius":0.0,"color":3355508484,"fillIntensity":0.345,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北2","refX":107.778175,"refY":92.221825,"radius":0.3,"color":3355508484,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北2-3","type":2,"refX":107.986694,"refY":92.46706,"refZ":-3.8146973E-06,"offX":110.93152,"offY":98.19224,"offZ":-3.8146973E-06,"radius":0.0,"color":3355508484,"fillIntensity":0.345,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北3","refX":111.0,"refY":98.5,"radius":0.3,"color":3355508484,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北3-4","type":2,"refX":110.869156,"refY":98.76253,"offX":107.1981,"offY":103.79292,"radius":0.0,"color":3355508484,"fillIntensity":0.345,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北4","refX":107.0,"refY":104.0,"radius":0.3,"color":3355508484,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北4-5","type":2,"refX":106.88246,"refY":104.25977,"offX":103.307785,"offY":109.23871,"radius":0.0,"color":3355508484,"fillIntensity":0.345,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北5","refX":103.2,"refY":109.5,"refZ":-7.6293945E-06,"radius":0.3,"color":3355508484,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"6m","Enabled":false,"refX":93.0,"refY":96.0,"radius":6.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"円周","Enabled":false,"refX":100.0,"refY":100.0,"radius":10.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":15.3,"Match":"(12809>40212)","MatchDelay":4.7}]}
~Lv2~{"Name":"P2_光の暴走：南北ライン","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"","type":2,"refX":100.0,"refY":80.0,"offX":100.0,"offY":120.0,"radius":0.0,"Filled":false,"fillIntensity":0.345,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":6.0,"Match":"(12809>40212)","MatchDelay":15.0}]}
~Lv2~{"Name":"P2_聖なる光：AoE予兆","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"AoE","type":1,"radius":11.0,"fillIntensity":0.5,"thicc":0.0,"refActorNPCNameID":9318,"refActorRequireCast":true,"refActorCastId":[40219],"refActorUseCastTime":true,"refActorCastTimeMin":2.0,"refActorCastTimeMax":4.7,"refActorComparisonType":6,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"予兆","type":1,"radius":11.0,"color":3372218624,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":9318,"refActorComparisonType":6,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
~Lv2~{"Name":"P2_光の暴走：重光の兆し","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"頭割りカウントダウン3","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[4159],"refActorUseBuffTime":true,"refActorBuffTimeMin":2.0,"refActorBuffTimeMax":3.0,"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"頭割りカウントダウン2","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508223,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[4159],"refActorUseBuffTime":true,"refActorBuffTimeMin":1.0,"refActorBuffTimeMax":2.0,"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"頭割りカウントダウン1","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[4159],"refActorUseBuffTime":true,"refActorBuffTimeMax":1.0,"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"頭割り","type":1,"radius":5.0,"color":3372217088,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[4159],"refActorUseBuffTime":true,"refActorBuffTimeMax":5.0,"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
~Lv2~{"Name":"P2_シヴァ・ミトロン：光の津波_AOE","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"前方扇1","type":4,"radius":20.0,"coneAngleMin":-30,"coneAngleMax":30,"color":4278190335,"fillIntensity":0.104,"thicc":3.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40189],"refActorComparisonType":6,"includeHitbox":true,"includeRotation":true,"onlyVisible":true,"FaceMe":true,"DistanceMax":13.2,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":4.0},{"Name":"前方扇2","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-30,"coneAngleMax":30,"color":4278190335,"fillIntensity":0.099,"thicc":3.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40189],"refActorComparisonType":6,"includeHitbox":true,"includeRotation":true,"onlyVisible":true,"FaceMe":true,"DistanceMax":13.2,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"faceplayer":"<2>","FillStep":4.0},{"Name":"前方扇3","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-30,"coneAngleMax":30,"color":4278190335,"fillIntensity":0.103,"thicc":3.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40189],"refActorComparisonType":6,"includeHitbox":true,"includeRotation":true,"onlyVisible":true,"FaceMe":true,"DistanceMax":13.2,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"faceplayer":"<3>","FillStep":4.0},{"Name":"前方扇4","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-30,"coneAngleMax":30,"color":4278190335,"fillIntensity":0.104,"thicc":3.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40189],"refActorComparisonType":6,"includeHitbox":true,"includeRotation":true,"onlyVisible":true,"FaceMe":true,"DistanceMax":13.2,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"faceplayer":"<4>","FillStep":4.0},{"Name":"前方扇5","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-30,"coneAngleMax":30,"color":4278190335,"fillIntensity":0.099,"thicc":3.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40189],"refActorComparisonType":6,"includeHitbox":true,"includeRotation":true,"onlyVisible":true,"FaceMe":true,"DistanceMax":13.2,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"faceplayer":"<5>","FillStep":4.0},{"Name":"前方扇6","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-30,"coneAngleMax":30,"color":4278190335,"fillIntensity":0.093,"thicc":3.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40189],"refActorComparisonType":6,"includeHitbox":true,"includeRotation":true,"onlyVisible":true,"FaceMe":true,"DistanceMax":13.2,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"faceplayer":"<6>","FillStep":4.0},{"Name":"前方扇7","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-30,"coneAngleMax":30,"color":4278190335,"fillIntensity":0.108,"thicc":3.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40189],"refActorComparisonType":6,"includeHitbox":true,"includeRotation":true,"onlyVisible":true,"FaceMe":true,"DistanceMax":13.2,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"faceplayer":"<7>","FillStep":4.0},{"Name":"前方扇8","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-30,"coneAngleMax":30,"color":4278190335,"fillIntensity":0.088,"thicc":3.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40189],"refActorComparisonType":6,"includeHitbox":true,"includeRotation":true,"onlyVisible":true,"FaceMe":true,"DistanceMax":13.2,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"faceplayer":"<8>","FillStep":4.0}]}
~Lv2~{"Name":"P2_シヴァ・ミトロン：光の津波_散開位置","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"MT","type":1,"offY":5.0,"radius":1.0,"color":4294901770,"Filled":false,"fillIntensity":0.3,"overlayBGColor":4278190080,"overlayTextColor":4294901770,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":5.0,"overlayText":"MT","refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40189],"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"FaceMe":true,"DistanceMax":13.2,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":4.0},{"Name":"ST","type":1,"offY":-5.0,"radius":1.0,"color":4294901770,"Filled":false,"fillIntensity":0.3,"overlayBGColor":4278190080,"overlayTextColor":4294901770,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":5.0,"overlayText":"ST","refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40189],"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"FaceMe":true,"DistanceMax":13.2,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":4.0},{"Name":"PH","type":1,"offX":-5.0,"radius":1.0,"color":4278255370,"Filled":false,"fillIntensity":0.3,"overlayBGColor":4278190080,"overlayTextColor":4278255370,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":5.0,"overlayText":"PH","refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40189],"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"FaceMe":true,"DistanceMax":13.2,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":4.0},{"Name":"BH","type":1,"offX":5.0,"radius":1.0,"color":4278255370,"Filled":false,"fillIntensity":0.3,"overlayBGColor":4278190080,"overlayTextColor":4278255370,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":5.0,"overlayText":"BH","refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40189],"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"FaceMe":true,"DistanceMax":13.2,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":4.0},{"Name":"D1","type":1,"offX":-3.5,"offY":-3.5,"radius":1.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":4278190080,"overlayTextColor":4278190335,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":5.0,"overlayText":"D1","refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40189],"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"FaceMe":true,"DistanceMax":13.2,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":4.0},{"Name":"D2","type":1,"offX":3.5,"offY":-3.5,"radius":1.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":4278190080,"overlayTextColor":4278190335,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":5.0,"overlayText":"D2","refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40189],"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"FaceMe":true,"DistanceMax":13.2,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":4.0},{"Name":"D3","type":1,"offX":-3.5,"offY":3.5,"radius":1.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":4278190080,"overlayTextColor":4278190335,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":5.0,"overlayText":"D3","refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40189],"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"FaceMe":true,"DistanceMax":13.2,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":4.0},{"Name":"D4","type":1,"offX":3.5,"offY":3.5,"radius":1.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":4278190080,"overlayTextColor":4278190335,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":5.0,"overlayText":"D4","refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40189],"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"FaceMe":true,"DistanceMax":13.2,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":4.0}]}
~Lv2~{"Enabled":false,"Name":"P2_聖なる光：AoE着弾警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"AoE警告","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"AoE着弾まで...","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"(12809>40212)","MatchDelay":19.0}]}
~Lv2~{"Enabled":false,"Name":"P2_聖なる光：AoE着弾カウントダウン5","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508577,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"5","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40212)","MatchDelay":19.0}]}
~Lv2~{"Enabled":false,"Name":"P2_聖なる光：AoE着弾カウントダウン4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508577,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40212)","MatchDelay":20.0}]}
~Lv2~{"Enabled":false,"Name":"P2_聖なる光：AoE着弾カウントダウン3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508577,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40212)","MatchDelay":21.0}]}
~Lv2~{"Enabled":false,"Name":"P2_聖なる光：AoE着弾カウントダウン2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508719,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40212)","MatchDelay":22.0}]}
~Lv2~{"Enabled":false,"Name":"P2_聖なる光：AoE着弾カウントダウン1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40212)","MatchDelay":23.0}]}
~Lv2~{"Name":"P2.5_闇水晶：シンブリザガ","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"前方扇","type":4,"radius":30.0,"coneAngleMin":-10,"coneAngleMax":10,"color":4278190335,"fillIntensity":0.3,"thicc":3.0,"refActorNPCNameID":13556,"refActorRequireCast":true,"refActorCastId":[40262],"refActorUseCastTime":true,"refActorCastTimeMax":5.0,"refActorComparisonType":6,"includeHitbox":true,"includeRotation":true,"DistanceMax":13.2,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":4.0}]}
~Lv2~{"Enabled":false,"Name":"P2.5_光水晶：AoE範囲","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"北","type":1,"radius":7.0,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":85.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"西","type":1,"radius":7.0,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"LimitDistance":true,"DistanceSourceX":85.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"東","type":1,"radius":7.0,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"LimitDistance":true,"DistanceSourceX":115.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南","type":1,"radius":7.0,"color":3357277952,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":115.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":39.7,"Match":"(9358>40259)"}]}
~Lv2~{"Enabled":false,"Name":"P2.5_光水晶：1回目AoEカウントダウン3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"北","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508558,"overlayVOffset":2.5,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":85.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"西","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508558,"overlayVOffset":2.5,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"LimitDistance":true,"DistanceSourceX":85.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"東","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508558,"overlayVOffset":2.5,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"LimitDistance":true,"DistanceSourceX":115.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508558,"overlayVOffset":2.5,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":115.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9358>40259)","MatchDelay":3.0}]}
~Lv2~{"Enabled":false,"Name":"P2.5_光水晶：1回目AoEカウントダウン2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"北","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508712,"overlayVOffset":2.5,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>","<1>"],"refActorComparisonType":5,"refActorType":1,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":85.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"西","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508223,"overlayVOffset":2.5,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"LimitDistance":true,"DistanceSourceX":85.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"東","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508223,"overlayVOffset":2.5,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"LimitDistance":true,"DistanceSourceX":115.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508712,"overlayVOffset":2.5,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":115.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9358>40259)","MatchDelay":4.0}]}
~Lv2~{"Enabled":false,"Name":"P2.5_光水晶：1回目AoEカウントダウン1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"北","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":2.5,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":85.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"西","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":2.5,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"LimitDistance":true,"DistanceSourceX":85.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"東","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":2.5,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"LimitDistance":true,"DistanceSourceX":115.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":2.5,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":115.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9358>40259)","MatchDelay":5.0}]}
~Lv2~{"Enabled":false,"Name":"P2.5_光水晶：1回目AoE警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"北","Enabled":false,"refX":100.0,"refY":85.0,"radius":5.0,"fillIntensity":0.5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"西","Enabled":false,"refX":85.0,"refY":100.0,"radius":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"東","Enabled":false,"refX":115.0,"refY":100.0,"radius":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南","Enabled":false,"refX":100.0,"refY":115.0,"radius":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北：警告","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":1.5,"overlayFScale":2.0,"thicc":0.0,"overlayText":"1回目AoEまで","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":85.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"西：警告","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":1.5,"overlayFScale":2.0,"thicc":0.0,"overlayText":"1回目AoEまで","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"LimitDistance":true,"DistanceSourceX":85.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"東：警告","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":1.5,"overlayFScale":2.0,"thicc":0.0,"overlayText":"1回目AoEまで","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"LimitDistance":true,"DistanceSourceX":115.0,"DistanceSourceY":100.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南：警告","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":1.5,"overlayFScale":2.0,"thicc":0.0,"overlayText":"1回目AoEまで","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"LimitDistance":true,"DistanceSourceX":100.0,"DistanceSourceY":115.0,"DistanceMax":5.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":3.0,"Match":"(9358>40259)","MatchDelay":3.0}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"ダークファイガ_AOE","type":1,"radius":10.0,"color":4278255401,"Filled":false,"fillIntensity":0.146,"overlayBGColor":3355443200,"overlayTextColor":4278255401,"overlayFScale":1.5,"thicc":4.0,"overlayText":"外周へ","refActorRequireBuff":true,"refActorBuffId":[2455],"refActorUseBuffTime":true,"refActorBuffTimeMax":5.0,"refActorComparisonType":1,"refActorType":1,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ダークファイガ_CD1","type":1,"Enabled":false,"radius":0.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278190335,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"1","refActorRequireBuff":true,"refActorBuffId":[2455],"refActorUseBuffTime":true,"refActorBuffTimeMax":1.0,"refActorComparisonType":1,"refActorType":1,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ダークファイガ_CD2","type":1,"Enabled":false,"radius":0.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278253567,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"2","refActorRequireBuff":true,"refActorBuffId":[2455],"refActorUseBuffTime":true,"refActorBuffTimeMin":1.0,"refActorBuffTimeMax":2.0,"refActorComparisonType":1,"refActorType":1,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ダークファイガ_CD3","type":1,"Enabled":false,"radius":0.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278255383,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"3","refActorRequireBuff":true,"refActorBuffId":[2455],"refActorUseBuffTime":true,"refActorBuffTimeMin":2.0,"refActorBuffTimeMax":3.0,"refActorComparisonType":1,"refActorType":1,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ダークファイガ_CD4","type":1,"Enabled":false,"radius":0.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278255420,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"4","refActorRequireBuff":true,"refActorBuffId":[2455],"refActorUseBuffTime":true,"refActorBuffTimeMin":3.0,"refActorBuffTimeMax":4.0,"refActorComparisonType":1,"refActorType":1,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ダークファイガ_CD5","type":1,"Enabled":false,"radius":0.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278255426,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"5","refActorRequireBuff":true,"refActorBuffId":[2455],"refActorUseBuffTime":true,"refActorBuffTimeMin":4.0,"refActorBuffTimeMax":5.0,"refActorComparisonType":1,"refActorType":1,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ダークブリザガ_AOE","type":1,"radius":3.0,"Donut":10.0,"color":4290117376,"fillIntensity":0.103,"overlayBGColor":3355443200,"overlayTextColor":4294573824,"overlayFScale":1.5,"overlayText":"中へ","refActorRequireBuff":true,"refActorBuffId":[2462],"refActorUseBuffTime":true,"refActorBuffTimeMax":5.0,"refActorComparisonType":1,"refActorType":1,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ダークブリザガ_CD1","type":1,"Enabled":false,"radius":0.0,"Donut":10.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278190335,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"1","refActorRequireBuff":true,"refActorBuffId":[2462],"refActorUseBuffTime":true,"refActorBuffTimeMax":1.0,"refActorComparisonType":1,"refActorType":1,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ダークブリザガ_CD2","type":1,"Enabled":false,"radius":0.0,"Donut":10.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278255611,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"2","refActorRequireBuff":true,"refActorBuffId":[2462],"refActorUseBuffTime":true,"refActorBuffTimeMin":1.0,"refActorBuffTimeMax":2.0,"refActorComparisonType":1,"refActorType":1,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ダークブリザガ_CD3","type":1,"Enabled":false,"radius":0.0,"Donut":10.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278255395,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"3","refActorRequireBuff":true,"refActorBuffId":[2462],"refActorUseBuffTime":true,"refActorBuffTimeMin":2.0,"refActorBuffTimeMax":3.0,"refActorComparisonType":1,"refActorType":1,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ダークブリザガ_CD4","type":1,"Enabled":false,"radius":0.0,"Donut":10.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278255413,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"4","refActorRequireBuff":true,"refActorBuffId":[2462],"refActorUseBuffTime":true,"refActorBuffTimeMin":3.0,"refActorBuffTimeMax":4.0,"refActorComparisonType":1,"refActorType":1,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ダークブリザガ_CD5","type":1,"Enabled":false,"radius":0.0,"Donut":10.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278255407,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"5","refActorRequireBuff":true,"refActorBuffId":[2462],"refActorUseBuffTime":true,"refActorBuffTimeMin":4.0,"refActorBuffTimeMax":5.0,"refActorComparisonType":1,"refActorType":1,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ダークホーリー_AOE","type":1,"radius":4.8,"Donut":0.2,"color":4294967040,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4294967040,"overlayFScale":1.5,"thicc":6.0,"overlayText":"中へ","refActorRequireBuff":true,"refActorBuffId":[2454],"refActorUseBuffTime":true,"refActorBuffTimeMax":5.0,"refActorComparisonType":1,"refActorType":1,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ダークホーリー_CD1","type":1,"Enabled":false,"radius":0.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278190335,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"1","refActorRequireBuff":true,"refActorBuffId":[2454],"refActorUseBuffTime":true,"refActorBuffTimeMax":1.0,"refActorComparisonType":1,"refActorType":1,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ダークホーリー_CD2","type":1,"Enabled":false,"radius":0.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278255605,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"2","refActorRequireBuff":true,"refActorBuffId":[2454],"refActorUseBuffTime":true,"refActorBuffTimeMin":1.0,"refActorBuffTimeMax":2.0,"refActorComparisonType":1,"refActorType":1,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ダークホーリー_CD3","type":1,"Enabled":false,"radius":0.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278255444,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"3","refActorRequireBuff":true,"refActorBuffId":[2454],"refActorUseBuffTime":true,"refActorBuffTimeMin":2.0,"refActorBuffTimeMax":3.0,"refActorComparisonType":1,"refActorType":1,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ダークホーリー_CD4","type":1,"Enabled":false,"radius":0.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278255450,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"4","refActorRequireBuff":true,"refActorBuffId":[2454],"refActorUseBuffTime":true,"refActorBuffTimeMin":3.0,"refActorBuffTimeMax":4.0,"refActorComparisonType":1,"refActorType":1,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ダークホーリー_CD5","type":1,"Enabled":false,"radius":0.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":4278190080,"overlayTextColor":3355508564,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"5","refActorRequireBuff":true,"refActorBuffId":[2454],"refActorUseBuffTime":true,"refActorBuffTimeMin":4.0,"refActorBuffTimeMax":5.0,"refActorComparisonType":1,"refActorType":1,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"誘導位置_左回り","type":1,"offX":-2.4,"offY":-0.8,"radius":0.5,"color":4278255413,"Filled":false,"fillIntensity":0.3,"thicc":5.0,"refActorRequireCast":true,"refActorCastId":[40291],"refActorComparisonType":7,"includeRotation":true,"onlyVisible":true,"refActorVFXPath":"vfx/common/eff/m0489_stlp_left01f_c0d1.avfx","refActorVFXMax":10000,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":2.0},{"Name":"誘導位置_右回り","type":1,"offX":2.4,"offY":-0.8,"radius":0.5,"color":4278255413,"Filled":false,"fillIntensity":0.3,"thicc":5.0,"refActorRequireCast":true,"refActorCastId":[40291],"refActorComparisonType":7,"includeRotation":true,"onlyVisible":true,"refActorVFXPath":"vfx/common/eff/m0489_stlp_right_c0d1.avfx","refActorVFXMax":10000,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":2.0},{"Name":"砂時計(quick)Line","type":3,"refY":-11.0,"offY":9.5,"radius":0.0,"color":3355506687,"Filled":false,"fillIntensity":0.345,"thicc":16.0,"refActorPlaceholder":[],"refActorNPCNameID":9825,"refActorComparisonAnd":true,"refActorComparisonType":7,"includeRotation":true,"onlyVisible":true,"refActorVFXPath":"vfx/channeling/eff/chn_d1049_quick01k1.avfx","refActorVFXMax":40000,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"砂時計(slow)Line","type":3,"refY":-11.0,"offY":9.5,"radius":0.0,"color":3372155045,"Filled":false,"fillIntensity":0.345,"thicc":16.0,"refActorPlaceholder":[],"refActorNPCNameID":9825,"refActorComparisonAnd":true,"refActorComparisonType":7,"includeRotation":true,"onlyVisible":true,"refActorVFXPath":"vfx/channeling/eff/chn_d1049_slow01k1.avfx","refActorVFXMax":40000,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"砂時計","type":1,"offY":3.0,"radius":0.0,"color":3355508719,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355508719,"overlayTextColor":3355443455,"overlayFScale":4.0,"thicc":0.0,"overlayText":"誘導","refActorNPCNameID":9825,"refActorRequireBuff":true,"refActorBuffId":[2970],"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":54.0,"Match":"(9832>40266)"}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_早リターン_Step2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"エラプ","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3372220415,"overlayTextColor":3372154884,"overlayFScale":1.5,"thicc":0.0,"overlayText":"時計の下","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[2460],"refActorUseBuffTime":true,"refActorBuffTimeMin":27.0,"refActorBuffTimeMax":32.0,"refActorComparisonType":5,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ウォタガ","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3372220415,"overlayTextColor":3372154884,"overlayFScale":1.5,"thicc":0.0,"overlayText":"中へ","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[2461],"refActorUseBuffTime":true,"refActorBuffTimeMin":27.0,"refActorBuffTimeMax":32.0,"refActorComparisonType":5,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"(9832>40266)","MatchDelay":21.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_遅リターン_Step4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"リターン","type":1,"radius":0.0,"overlayBGColor":3372220415,"overlayTextColor":3372154884,"overlayFScale":1.5,"thicc":0.0,"overlayText":"中へ","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[2464],"refActorUseBuffTime":true,"refActorBuffTimeMax":5.0,"refActorComparisonType":5,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"(9832>40266)","MatchDelay":31.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_シェルクラッシャー","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"stack","refX":100.0,"refY":100.0,"radius":5.8,"Donut":0.2,"color":3372213760,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3372213760,"overlayFScale":2.0,"thicc":4.0,"overlayText":"Stack","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"(9832>40266)","MatchDelay":52.0}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.1_警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"Step1","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508527,"overlayVOffset":1.0,"overlayFScale":1.2,"thicc":0.0,"overlayText":"Step.1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"(9832>40266)","MatchDelay":16.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.1_CD5","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"5","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":16.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.1_CD4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":17.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.1_CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":18.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.1_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508706,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":19.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.1_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":20.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.2_警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"Step2","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508527,"overlayVOffset":1.0,"overlayFScale":1.2,"thicc":0.0,"overlayText":"Step.2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"(9832>40266)","MatchDelay":21.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.2_CD5","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"5","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":21.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.2_CD4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":22.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.2_CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":23.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.2_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508223,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":24.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.2_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":25.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.3_警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"Step3","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508527,"overlayVOffset":1.0,"overlayFScale":1.2,"thicc":0.0,"overlayText":"Step.3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"(9832>40266)","MatchDelay":26.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.3_CD5","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"5","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":26.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.3_CD4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":27.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.3_CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":28.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.3_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508719,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":29.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.3_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":30.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.4_警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"Step4","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508527,"overlayVOffset":1.0,"overlayFScale":1.2,"thicc":0.0,"overlayText":"Step.4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"(9832>40266)","MatchDelay":31.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.4_CD5","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"5","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":31.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.4_CD4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":32.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.4_CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":33.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.4_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508223,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":34.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.4_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":35.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.5_警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"Step5","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508527,"overlayVOffset":1.0,"overlayFScale":1.2,"thicc":0.0,"overlayText":"Step.5","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"(9832>40266)","MatchDelay":36.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.5_CD5","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"5","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":36.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.5_CD4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":37.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.5_CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":38.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.5_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508223,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":39.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.5_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":40.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.6_警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"Step6","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508527,"overlayVOffset":1.0,"overlayFScale":1.2,"thicc":0.0,"overlayText":"Step.6","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"(9832>40266)","MatchDelay":41.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.6_CD5","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"5","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":41.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.6_CD4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":42.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.6_CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":43.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.6_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508223,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":44.5}]}
~Lv2~{"Name":"P3_闇の巫女：時間圧縮・絶_Step.6_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40266)","MatchDelay":45.5}]}
~Lv2~{"Name":"P3_アポカリプス：初期位置(固定1-3)","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"TH1  (8.5 ,  45+/-10)","refX":106.96279,"refY":95.1246,"radius":1.0,"color":3355508521,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"TH2","refX":104.8754,"refY":93.03721,"radius":1.0,"color":3355508521,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"TH3  (11 ,  45+/-8)","refX":108.78499,"refY":93.380035,"radius":1.0,"color":3355508521,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"TH4","refX":106.619965,"refY":91.21501,"refZ":-1.9073486E-06,"radius":1.0,"color":3355508521,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"DPS1","refX":93.03721,"refY":104.8754,"radius":1.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"DPS2","refX":95.1246,"refY":106.96279,"radius":1.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"DPS3","refX":91.21501,"refY":106.619965,"radius":1.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"DPS4","refX":93.380035,"refY":108.78499,"radius":1.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":27.0,"Match":"(9832>40290)","MatchDelay":4.7}]}
~Lv2~{"Enabled":false,"Name":"P3_アポカリプス：初期位置(しのしょー)","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"TH1","refX":108.21037,"refY":97.80004,"radius":1.0,"color":3355508521,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"TH2","refX":106.96279,"refY":95.1246,"radius":1.0,"color":3355508521,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"TH3","refX":110.519356,"refY":96.78391,"radius":1.0,"color":3355508521,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"TH4","refX":109.22538,"refY":94.00897,"radius":1.0,"color":3355508521,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"DPS1","refX":91.78963,"refY":102.19996,"radius":1.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"DPS2","refX":93.03721,"refY":104.8754,"radius":1.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"DPS3","refX":89.480644,"refY":103.21609,"radius":1.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"DPS4","refX":90.77462,"refY":105.99103,"radius":1.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":27.0,"Match":"(9832>40290)","MatchDelay":4.7}]}
~Lv2~{"Enabled":false,"Name":"P3_アポカリプス：初期位置(リリド)","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"TH1","refX":97.80004,"refY":91.78963,"radius":1.0,"color":3355508521,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"TH2","refX":95.1246,"refY":93.03721,"radius":1.0,"color":3355508521,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"TH3","refX":96.78391,"refY":89.480644,"radius":1.0,"color":3355508521,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"TH4","refX":94.00897,"refY":90.77462,"radius":1.0,"color":3355508521,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"DPS1","refX":102.19996,"refY":108.21037,"radius":1.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"DPS2","refX":104.8754,"refY":106.96279,"radius":1.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"DPS3","refX":103.21609,"refY":110.519356,"radius":1.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"DPS4","refX":105.99103,"refY":109.22538,"radius":1.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":27.0,"Match":"(9832>40290)","MatchDelay":4.7}]}
~Lv2~{"Name":"P3_アポカリプス：デバフ(早中遅)","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"早","type":1,"radius":0.5,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"thicc":4.0,"overlayText":"1回目","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[2461],"refActorUseBuffTime":true,"refActorBuffTimeMin":5.0,"refActorBuffTimeMax":10.0,"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"中","type":1,"radius":0.5,"color":3355508725,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508725,"thicc":4.0,"overlayText":"2回目","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[2461],"refActorUseBuffTime":true,"refActorBuffTimeMin":24.0,"refActorBuffTimeMax":29.0,"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"遅","type":1,"radius":0.5,"color":3370188544,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3370188544,"thicc":4.0,"overlayText":"3回目","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[2461],"refActorUseBuffTime":true,"refActorBuffTimeMin":33.0,"refActorBuffTimeMax":38.0,"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"無職","type":1,"radius":0.5,"color":3372220415,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"thicc":4.0,"overlayText":"無職","refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>","<1>"],"refActorRequireBuff":true,"refActorBuffId":[2461],"refActorRequireAllBuffs":true,"refActorRequireBuffsInvert":true,"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"(9832>40269)","MatchDelay":10.0}]}
~Lv2~{"Name":"P3_アポカリプス：1回目頭割り(警告)","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1.Stack","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3370974976,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1.Stack","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"(9832>40269)","MatchDelay":15.7}]}
~Lv2~{"Name":"P3_アポカリプス：1回目頭割り_CD5","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"5","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40269)","MatchDelay":15.7}]}
~Lv2~{"Name":"P3_アポカリプス：1回目頭割り_CD4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40269)","MatchDelay":16.7}]}
~Lv2~{"Name":"P3_アポカリプス：1回目頭割り_CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40269)","MatchDelay":17.7}]}
~Lv2~{"Name":"P3_アポカリプス：1回目頭割り_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508223,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40269)","MatchDelay":18.7}]}
~Lv2~{"Name":"P3_アポカリプス：1回目頭割り_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40269)","MatchDelay":19.7}]}
~Lv2~{"Name":"P3_アポカリプス：テイカー散開(警告)","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2.Spread","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508509,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2.Spread","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":2.0,"Match":"(9832>40269)","MatchDelay":20.7}]}
~Lv2~{"Name":"P3_アポカリプス：テイカー散開_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508223,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40269)","MatchDelay":20.7}]}
~Lv2~{"Name":"P3_アポカリプス：テイカー散開_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40269)","MatchDelay":21.7}]}
~Lv2~{"Name":"P3_アポカリプス：エラプション散開(警告)","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3.Spread","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508509,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3.Spread","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":4.7,"Match":"(9832>40273)"}]}
~Lv2~{"Name":"P3_アポカリプス：エラプション散開_CD5","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508527,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"5","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":0.7,"Match":"(9832>40273)"}]}
~Lv2~{"Name":"P3_アポカリプス：エラプション散開_CD4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508527,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40273)","MatchDelay":0.7}]}
~Lv2~{"Name":"P3_アポカリプス：エラプション散開_CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508527,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40273)","MatchDelay":1.7}]}
~Lv2~{"Name":"P3_アポカリプス：エラプション散開_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508731,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40273)","MatchDelay":2.7}]}
~Lv2~{"Name":"P3_アポカリプス：エラプション散開_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40273)","MatchDelay":3.7}]}
~Lv2~{"Name":"P3_アポカリプス：2回目頭割り(警告)","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4.Stack","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3370974976,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"4.Stack","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"(9832>40273)","MatchDelay":5.8}]}
~Lv2~{"Name":"P3_アポカリプス：2回目頭割り_CD5","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508527,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"5","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40273)","MatchDelay":5.8}]}
~Lv2~{"Name":"P3_アポカリプス：2回目頭割り_CD4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508527,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40273)","MatchDelay":6.8}]}
~Lv2~{"Name":"P3_アポカリプス：2回目頭割り_CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508527,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40273)","MatchDelay":7.8}]}
~Lv2~{"Name":"P3_アポカリプス：2回目頭割り_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508725,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40273)","MatchDelay":8.8}]}
~Lv2~{"Name":"P3_アポカリプス：2回目頭割り_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40273)","MatchDelay":9.8}]}
~Lv2~{"Name":"P3_アポカリプス：3回目頭割り(警告)","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5.Stack","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3370974976,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"5.Stack","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"(9832>40273)","MatchDelay":14.8}]}
~Lv2~{"Name":"P3_アポカリプス：3回目頭割り_CD5","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508527,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"5","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40273)","MatchDelay":14.8}]}
~Lv2~{"Name":"P3_アポカリプス：3回目頭割り_CD4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508527,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40273)","MatchDelay":15.8}]}
~Lv2~{"Name":"P3_アポカリプス：3回目頭割り_CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508527,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40273)","MatchDelay":16.8}]}
~Lv2~{"Name":"P3_アポカリプス：3回目頭割り_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508719,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40273)","MatchDelay":17.8}]}
~Lv2~{"Name":"P3_アポカリプス：3回目頭割り_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(9832>40273)","MatchDelay":18.8}]}
~Lv2~{"Name":"P3_闇の巫女：ダークウォタガ","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"AOE","type":1,"radius":5.8,"Donut":0.2,"color":4294963968,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4294963968,"thicc":6.0,"overlayText":"Stack","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[2461],"refActorUseBuffTime":true,"refActorBuffTimeMax":5.0,"refActorComparisonType":5,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"CD1","type":1,"Enabled":false,"radius":0.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278190335,"overlayVOffset":1.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"1","refActorRequireBuff":true,"refActorBuffId":[2461],"refActorUseBuffTime":true,"refActorBuffTimeMax":1.0,"refActorComparisonType":1,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"CD2","type":1,"Enabled":false,"radius":0.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278254846,"overlayVOffset":1.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"2","refActorRequireBuff":true,"refActorBuffId":[2461],"refActorUseBuffTime":true,"refActorBuffTimeMin":1.0,"refActorBuffTimeMax":2.0,"refActorComparisonType":1,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"CD3","type":1,"Enabled":false,"radius":0.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278255611,"overlayVOffset":1.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"3","refActorRequireBuff":true,"refActorBuffId":[2461],"refActorUseBuffTime":true,"refActorBuffTimeMin":2.0,"refActorBuffTimeMax":3.0,"refActorComparisonType":1,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"CD4","type":1,"Enabled":false,"radius":0.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278255383,"overlayVOffset":1.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"4","refActorRequireBuff":true,"refActorBuffId":[2461],"refActorUseBuffTime":true,"refActorBuffTimeMin":3.0,"refActorBuffTimeMax":4.0,"refActorComparisonType":1,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"CD5","type":1,"Enabled":false,"radius":0.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278255376,"overlayVOffset":1.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"5","refActorRequireBuff":true,"refActorBuffId":[2461],"refActorUseBuffTime":true,"refActorBuffTimeMin":4.0,"refActorBuffTimeMax":5.0,"refActorComparisonType":1,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
~Lv2~{"Name":"P3_闇の巫女：スピリットテイカー","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"サークル","type":1,"radius":5.0,"color":4278255389,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278255389,"thicc":8.0,"overlayText":"Spread","refActorPlaceholder":["<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":4.0,"Match":"(9832>40288)","MatchDelay":1.0}]}
~Lv2~{"Name":"P3_闇の巫女：暗夜の舞踏技","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"ノックバック","type":1,"radius":0.0,"color":4278255103,"Filled":false,"fillIntensity":0.5,"overlayBGColor":4278190080,"overlayTextColor":4294967295,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":6.0,"refActorNPCNameID":9832,"refActorComparisonType":6,"onlyTargetable":true,"tether":true,"ExtraTetherLength":20.0,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":3.0,"Match":"(9832>40181)","MatchDelay":6.0}]}
~Lv2~{"Name":"P4_シヴァ・ミトロン：アクラーイ","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"Scenes":[6],"DCond":5,"ElementsL":[{"Name":"サークル","type":1,"radius":5.0,"color":4278190335,"fillIntensity":0.104,"refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":5.0}],"Freezing":true,"FreezeFor":8.0}
~Lv2~{"Name":"P4_シヴァ・ミトロン：アクラーイ_警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"警告","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508484,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"アク・ラーイ","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"}}]}
~Lv2~{"Name":"P4_シヴァ・ミトロン：アクラーイ_CD5","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"5","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"}}]}
~Lv2~{"Name":"P4_シヴァ・ミトロン：アクラーイ_CD4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":1.0}]}
~Lv2~{"Name":"P4_シヴァ・ミトロン：アクラーイ_CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":2.0}]}
~Lv2~{"Name":"P4_シヴァ・ミトロン：アクラーイ_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508725,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":3.0}]}
~Lv2~{"Name":"P4_シヴァ・ミトロン：アクラーイ_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":4.0}]}
~Lv2~{"Name":"P4_シヴァ・ミトロン：光の波動","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"前方扇","type":4,"Enabled":false,"radius":20.0,"coneAngleMin":-30,"coneAngleMax":30,"color":4278190335,"fillIntensity":0.3,"thicc":3.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40187],"refActorComparisonType":6,"includeHitbox":true,"includeRotation":true,"onlyVisible":true,"FaceMe":true,"DistanceMax":13.2,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":4.0},{"Name":"誘導位置_右前","type":1,"offX":2.82,"offY":2.0,"radius":0.5,"color":4278255384,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40187],"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"誘導位置_右後","type":1,"offX":2.82,"offY":-2.0,"radius":0.5,"color":4278255384,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40187],"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"誘導位置_左前","type":1,"offX":-2.82,"offY":2.0,"radius":0.5,"color":4278255384,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40187],"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"誘導位置_左後","type":1,"offX":-2.82,"offY":-2.0,"radius":0.5,"color":4278255384,"Filled":false,"fillIntensity":0.5,"thicc":5.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40187],"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
~Lv2~{"Name":"P4_シヴァ・ミトロン：ホーリーウィング","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"左半面","type":3,"refY":30.0,"radius":30.0,"color":4278190335,"fillIntensity":0.306,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40227],"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"AdditionalRotation":4.712389,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":2.0},{"Name":"右半面","type":3,"refY":30.0,"radius":30.0,"color":4278190335,"fillIntensity":0.3,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40228],"refActorComparisonType":6,"includeRotation":true,"AdditionalRotation":1.5707964,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"FillStep":2.0},{"Name":"左半面：頭割り1","type":1,"offX":4.0,"offY":-6.0,"radius":1.0,"color":3371433728,"Filled":false,"fillIntensity":0.5,"thicc":8.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40227],"refActorComparisonType":6,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"左半面：頭割り2","type":1,"offX":4.0,"offY":8.0,"radius":1.0,"color":3371433728,"Filled":false,"fillIntensity":0.5,"thicc":8.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40227],"refActorComparisonType":6,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"右半面：頭割り1","type":1,"offX":-4.0,"offY":-6.0,"radius":1.0,"color":3371433728,"Filled":false,"fillIntensity":0.5,"thicc":8.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40228],"refActorComparisonType":6,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"右半面：頭割り2","type":1,"offX":-4.0,"offY":8.0,"radius":1.0,"color":3371433728,"Filled":false,"fillIntensity":0.5,"thicc":8.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40228],"refActorComparisonType":6,"onlyVisible":true,"tether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
~Lv2~{"Name":"P4_シヴァ・ミトロン：アク・モーン","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"サークル","type":1,"radius":3.8,"Donut":0.2,"color":4294965504,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4294965504,"thicc":5.0,"overlayText":"Stack","refActorPlaceholder":["<t1>","<t2>"],"refActorComparisonType":5,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":9.0,"Match":"(12809>40247)"}]}
~Lv2~{"Name":"P4_シヴァ・ミトロン：モーン・アファー_警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"サークル","type":1,"Enabled":false,"radius":3.8,"Donut":0.2,"color":4294965504,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4294965504,"thicc":4.0,"overlayText":"Stack","refActorPlaceholder":["<t1>","<t2>"],"refActorComparisonType":5,"refActorType":1,"includeRotation":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"サークル(フィールド中心)","refX":100.0,"refY":100.0,"radius":3.8,"Donut":0.2,"color":3371826944,"fillIntensity":0.17,"overlayBGColor":3355443200,"overlayTextColor":3371826944,"overlayFScale":2.0,"thicc":4.0,"overlayText":"Stack","tether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"警告","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508484,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"モーン・アファー","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.7,"Match":"(12809>40249)"}]}
~Lv2~{"Name":"P4_シヴァ・ミトロン：モーン・アファー_CD5","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"5","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40249)","MatchDelay":0.7}]}
~Lv2~{"Name":"P4_シヴァ・ミトロン：モーン・アファー_CD4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40249)","MatchDelay":1.7}]}
~Lv2~{"Name":"P4_シヴァ・ミトロン：モーン・アファー_CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40249)","MatchDelay":2.7}]}
~Lv2~{"Name":"P4_シヴァ・ミトロン：モーン・アファー_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508719,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40249)","MatchDelay":3.7}]}
~Lv2~{"Name":"P4_シヴァ・ミトロン：モーン・アファー_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40249)","MatchDelay":4.7}]}
~Lv2~{"Name":"P4_未来の欠片：当たり判定","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"Scenes":[7,6],"DCond":5,"ElementsL":[{"Name":"サークル","type":1,"radius":5.0,"color":4278255103,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":13559,"refActorComparisonType":6,"includeHitbox":true,"includeRotation":true,"onlyVisible":true,"tether":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"Match":"(9832>40288)"},{"Type":2,"Duration":9.0,"Match":"(12809>40247)"}]}
~Lv2~{"Enabled":false,"Name":"P4_闇の巫女：出現警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"警告","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"Gaia POP","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":5.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":6.0}]}
~Lv2~{"Enabled":false,"Name":"P4_闇の巫女：出現_CD5","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508583,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"5","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":6.0}]}
~Lv2~{"Enabled":false,"Name":"P4_闇の巫女：出現_CD4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508583,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":7.0}]}
~Lv2~{"Enabled":false,"Name":"P4_闇の巫女：出現_CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508583,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":8.0}]}
~Lv2~{"Enabled":false,"Name":"P4_闇の巫女：出現_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508731,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":9.0}]}
~Lv2~{"Enabled":false,"Name":"P4_闇の巫女：出現_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":10.0}]}
~Lv2~{"Name":"P4_ガイア出現：強調表示","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"ガイア","type":1,"radius":1.5,"color":3355508712,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508712,"overlayFScale":3.0,"thicc":4.0,"overlayText":"ガイア","refActorNPCNameID":9832,"refActorComparisonType":6,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ミトロン","type":1,"radius":1.5,"color":3355639552,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355639552,"overlayFScale":3.0,"thicc":4.0,"overlayText":"ミトロン","refActorNPCNameID":12809,"refActorComparisonType":6,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":10.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":12.7}]}
~Lv2~{"Name":"P4_光の闇の竜詩：無職表示","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"無職強調表示","type":1,"radius":0.8,"color":3355505151,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355505151,"overlayVOffset":1.5,"overlayFScale":1.2,"thicc":4.0,"overlayText":"扇誘導","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[2253],"refActorRequireAllBuffs":true,"refActorRequireBuffsInvert":true,"refActorComparisonType":5,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":11.3,"Match":"(12809>40239)","MatchDelay":4.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：ダークウォタガ","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"Stack","type":1,"radius":1.0,"color":3370974976,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3370974976,"overlayVOffset":2.4,"overlayFScale":2.5,"thicc":4.0,"overlayText":"Stack","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[2461],"refActorComparisonType":5,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":16.0,"Match":"(12809>40239)"}]}
~Lv2~{"Name":"P4_光と闇の竜詩：テイカー散開位置","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"未来の欠片(6m)","type":1,"Enabled":false,"radius":6.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":13559,"refActorComparisonType":6,"includeHitbox":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"未来の欠片","type":1,"Enabled":false,"radius":0.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":13559,"refActorComparisonType":6,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北西塔組","refX":96.0,"refY":94.0,"radius":0.5,"color":3356032768,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北東塔組","refX":104.0,"refY":94.0,"refZ":1.9073486E-06,"radius":0.5,"color":3356032768,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南西塔組","refX":96.0,"refY":108.0,"refZ":7.6293945E-06,"radius":0.5,"color":3356032768,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南東塔組","refX":104.0,"refY":108.0,"refZ":-3.8146973E-06,"radius":0.5,"color":3356032768,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北西扇組","refX":90.0,"refY":96.0,"radius":0.5,"color":3356032768,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南西扇組","refX":90.0,"refY":104.0,"radius":0.5,"color":3356032768,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北東扇組","refX":110.0,"refY":96.0,"radius":0.5,"color":3356032768,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南東扇組","refX":110.0,"refY":104.0,"refZ":3.8146973E-06,"radius":0.5,"color":3356032768,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北西塔矢印","type":2,"refX":100.0,"refY":92.0,"offX":96.63096,"offY":93.76024,"offZ":1.9073486E-06,"radius":0.0,"color":3355508540,"fillIntensity":0.345,"thicc":4.0,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北東塔矢印","type":2,"refX":100.0,"refY":92.0,"offX":103.3969,"offY":93.6298,"radius":0.0,"color":3355508540,"fillIntensity":0.345,"thicc":4.0,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北西扇矢印","type":2,"refX":96.70692,"refY":97.894615,"refZ":-1.9073486E-06,"offX":90.55046,"offY":96.1355,"offZ":1.9073486E-06,"radius":0.0,"color":3355508540,"Filled":false,"fillIntensity":0.345,"thicc":4.0,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南西扇矢印","type":2,"refX":96.66579,"refY":102.06271,"offX":90.59069,"offY":103.93608,"radius":0.0,"color":3355508540,"fillIntensity":0.345,"thicc":4.0,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北東扇矢印","type":2,"refX":103.34367,"refY":97.86065,"refZ":-1.9073486E-06,"offX":109.30573,"offY":96.15503,"radius":0.0,"color":3355508540,"fillIntensity":0.345,"thicc":4.0,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南東扇矢印","type":2,"refX":103.3688,"refY":102.06102,"offX":109.341736,"offY":103.8557,"radius":0.0,"color":3355508540,"fillIntensity":0.345,"thicc":4.0,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南西塔矢印","type":2,"refX":100.0,"refY":108.0,"offX":96.5,"offY":108.0,"radius":0.0,"color":3355508540,"fillIntensity":0.345,"thicc":4.0,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南東塔矢印","type":2,"refX":100.0,"refY":108.0,"offX":103.5,"offY":108.0,"radius":0.0,"color":3355508540,"fillIntensity":0.345,"thicc":4.0,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":6.0,"Match":"(12809>40239)","MatchDelay":13.7}]}
~Lv2~{"Enabled":false,"Name":"P4_光と闇の竜詩：テイカー最小散開位置","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"未来の欠片(5m)","type":1,"Enabled":false,"radius":5.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":13559,"refActorComparisonType":6,"includeHitbox":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"未来の欠片","type":1,"Enabled":false,"radius":0.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCNameID":13559,"refActorComparisonType":6,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北西塔組","refX":96.0,"refY":92.5,"refZ":1.9073486E-06,"radius":0.3,"color":3356032768,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北東塔組","refX":104.0,"refY":92.5,"refZ":1.9073486E-06,"radius":0.3,"color":3356032768,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南西塔組","refX":96.0,"refY":108.0,"radius":0.3,"color":3356032768,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南東塔組","refX":104.0,"refY":108.0,"refZ":-3.8146973E-06,"radius":0.3,"color":3356032768,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北西扇組","refX":94.0,"refY":98.0,"refZ":-1.9073486E-06,"radius":0.3,"color":3356032768,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南西扇組","refX":92.0,"refY":104.0,"radius":0.3,"color":3356032768,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北東扇組","refX":106.0,"refY":98.0,"refZ":-1.9073486E-06,"radius":0.3,"color":3356032768,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南東扇組","refX":108.0,"refY":104.0,"refZ":1.9073486E-06,"radius":0.3,"color":3356032768,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北西塔矢印","type":2,"Enabled":false,"refX":100.0,"refY":92.0,"offX":96.63096,"offY":93.76024,"offZ":1.9073486E-06,"radius":0.0,"color":3355508540,"fillIntensity":0.345,"thicc":4.0,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北東塔矢印","type":2,"Enabled":false,"refX":100.0,"refY":92.0,"offX":103.3969,"offY":93.6298,"radius":0.0,"color":3355508540,"fillIntensity":0.345,"thicc":4.0,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北西扇矢印","type":2,"Enabled":false,"refX":96.70692,"refY":97.894615,"refZ":-1.9073486E-06,"offX":88.54826,"offY":96.12264,"offZ":1.9073486E-06,"radius":0.0,"color":3355508540,"Filled":false,"fillIntensity":0.345,"thicc":4.0,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南西扇矢印","type":2,"Enabled":false,"refX":96.66579,"refY":102.06271,"offX":88.53475,"offY":103.83628,"radius":0.0,"color":3355508540,"fillIntensity":0.345,"thicc":4.0,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"北東扇矢印","type":2,"Enabled":false,"refX":103.34367,"refY":97.86065,"refZ":-1.9073486E-06,"offX":111.46726,"offY":96.15464,"offZ":-9.536743E-07,"radius":0.0,"color":3355508540,"fillIntensity":0.345,"thicc":4.0,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南東扇矢印","type":2,"Enabled":false,"refX":103.3688,"refY":102.06102,"offX":111.48828,"offY":103.85141,"offZ":1.9073486E-06,"radius":0.0,"color":3355508540,"fillIntensity":0.345,"thicc":4.0,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南西塔矢印","type":2,"Enabled":false,"refX":100.0,"refY":108.0,"offX":96.5,"offY":108.0,"radius":0.0,"color":3355508540,"fillIntensity":0.345,"thicc":4.0,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南東塔矢印","type":2,"Enabled":false,"refX":100.0,"refY":108.0,"offX":103.5,"offY":108.0,"radius":0.0,"color":3355508540,"fillIntensity":0.345,"thicc":4.0,"LineEndB":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":6.0,"Match":"(12809>40239)","MatchDelay":13.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"警告","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508484,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"光と闇の竜詩","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":15.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":4.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：CD15","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"15","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"15","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":4.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：CD14","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"14","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"14","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":5.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：CD13","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"13","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"13","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":6.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：CD12","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"12","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"12","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":7.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：CD11","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"11","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"11","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":8.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：CD10","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"10","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"10","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":9.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：CD9","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"9","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"9","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":10.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：CD8","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"8","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"8","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":11.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：CD7","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"7","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"7","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":12.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：CD6","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"6","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"6","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"TimeBegin":7.0,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":13.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：CD5","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"5","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":14.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：CD4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":15.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":16.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508223,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":17.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":18.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：(しのしょー)","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"MT","refX":95.0,"refY":97.0,"radius":1.0,"color":3371826944,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3371826944,"overlayFScale":1.2,"thicc":4.0,"overlayText":"MT","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ST","refX":95.0,"refY":99.0,"radius":1.0,"color":3371826944,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3371826944,"overlayFScale":1.2,"thicc":4.0,"overlayText":"ST","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"PH","refX":95.0,"refY":101.0,"radius":1.0,"color":3357277952,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3357277952,"overlayFScale":1.2,"thicc":4.0,"overlayText":"PH","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"BH","refX":95.0,"refY":103.0,"radius":1.0,"color":3357277952,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3357277952,"overlayFScale":1.2,"thicc":4.0,"overlayText":"BH","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"D1","refX":105.0,"refY":97.0,"radius":1.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayFScale":1.2,"thicc":4.0,"overlayText":"D1","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"D2","refX":105.0,"refY":99.0,"radius":1.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayFScale":1.2,"thicc":4.0,"overlayText":"D2","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"D3","refX":105.0,"refY":101.0,"radius":1.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayFScale":1.2,"thicc":4.0,"overlayText":"D3","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"D4","refX":105.0,"refY":103.0,"radius":1.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayFScale":1.2,"thicc":4.0,"overlayText":"D4","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":15.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":4.7}]}
~Lv2~{"Enabled":false,"Name":"P4_光と闇の竜詩：(リリド)","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"MT","refX":103.0,"refY":95.0,"radius":1.0,"color":3371826944,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3371826944,"overlayFScale":1.2,"thicc":4.0,"overlayText":"MT","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"ST","refX":101.0,"refY":95.0,"radius":1.0,"color":3371826944,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3371826944,"overlayFScale":1.2,"thicc":4.0,"overlayText":"ST","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"PH","refX":99.0,"refY":95.0,"radius":1.0,"color":3357277952,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3357277952,"overlayFScale":1.2,"thicc":4.0,"overlayText":"PH","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"BH","refX":97.0,"refY":95.0,"radius":1.0,"color":3357277952,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3357277952,"overlayFScale":1.2,"thicc":4.0,"overlayText":"BH","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"D1","refX":97.0,"refY":105.0,"radius":1.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayFScale":1.2,"thicc":4.0,"overlayText":"D1","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"D2","refX":99.0,"refY":105.0,"radius":1.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayFScale":1.2,"thicc":4.0,"overlayText":"D2","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"D3","refX":101.0,"refY":105.0,"radius":1.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayFScale":1.2,"thicc":4.0,"overlayText":"D3","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"D4","refX":103.0,"refY":105.0,"radius":1.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayFScale":1.2,"thicc":4.0,"overlayText":"D4","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":15.0,"MatchIntl":{"En":"Come to me, Hraesvelgr!","Jp":"聖竜よ、来たれ……！"},"MatchDelay":4.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：扇&塔_警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"警告","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508484,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1.AoE+Tower","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":6.0,"Match":"(12809>40239)","MatchDelay":10.0}]}
~Lv2~{"Name":"P4_光と闇の竜詩：扇&塔_CD6","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"6","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"6","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":10.0}]}
~Lv2~{"Name":"P4_光と闇の竜詩：扇&塔_CD5","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"5","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":11.0}]}
~Lv2~{"Name":"P4_光と闇の竜詩：扇&塔_CD4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":12.0}]}
~Lv2~{"Name":"P4_光と闇の竜詩：扇&塔_CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":13.0}]}
~Lv2~{"Name":"P4_光と闇の竜詩：扇&塔_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508719,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":14.0}]}
~Lv2~{"Name":"P4_光と闇の竜詩：扇&塔_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":15.0}]}
~Lv2~{"Name":"P4_光と闇の竜詩：テイカー散開_警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"警告","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508484,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2.Spread","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":3.0,"Match":"(12809>40239)","MatchDelay":16.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：テイカー散開_CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"overlayBGColor":3355443200,"overlayTextColor":3355508583,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":16.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：テイカー散開_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508719,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":17.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：テイカー散開_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":18.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：頭割り_警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"警告","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3371826944,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3.Stack","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":4.0,"Match":"(12809>40239)","MatchDelay":20.0}]}
~Lv2~{"Name":"P4_光と闇の竜詩：頭割り_CD4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":20.0}]}
~Lv2~{"Name":"P4_光と闇の竜詩：頭割り_CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":21.0}]}
~Lv2~{"Name":"P4_光と闇の竜詩：頭割り_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355506687,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":22.0}]}
~Lv2~{"Name":"P4_光と闇の竜詩：頭割り_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":23.0}]}
~Lv2~{"Name":"P4_光と闇の竜詩：タンク強1_警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"警告","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508731,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"4.TankBuster1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":2.8,"Match":"(12809>40239)","MatchDelay":24.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：タンク強1_CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":0.8,"Match":"(12809>40239)","MatchDelay":24.7}]}
~Lv2~{"Name":"P4_光と闇の竜詩：タンク強1_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508731,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":25.5}]}
~Lv2~{"Name":"P4_光と闇の竜詩：タンク強1_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":26.5}]}
~Lv2~{"Name":"P4_光と闇の竜詩：タンク強2_警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"警告","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508731,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"5.TankBuster2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"闇の巫女 近誘導","type":4,"radius":1.5,"Donut":2.0,"coneAngleMin":-45,"coneAngleMax":45,"color":3355639552,"fillIntensity":0.314,"thicc":8.0,"refActorTargetingYou":2,"refActorNPCNameID":9832,"refTargetYou":true,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"闇の巫女 近誘導 text","type":1,"offY":2.5,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508449,"overlayFScale":2.0,"thicc":0.0,"overlayText":"誘導","refActorNPCID":9832,"refActorTargetingYou":2,"refTargetYou":true,"refActorComparisonType":4,"includeRotation":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"宵闇範囲","type":1,"radius":8.0,"Filled":false,"fillIntensity":0.5,"thicc":4.0,"refActorNPCID":9832,"refActorTargetingYou":1,"refTargetYou":true,"refActorComparisonType":4,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":3.0,"Match":"(12809>40239)","MatchDelay":27.5}]}
~Lv2~{"Name":"P4_光と闇の竜詩：タンク強2_CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508496,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":27.5}]}
~Lv2~{"Name":"P4_光と闇の竜詩：タンク強2_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508731,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":28.5}]}
~Lv2~{"Name":"P4_光と闇の竜詩：タンク強2_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":29.5}]}
~Lv2~{"Name":"P4_光と闇の竜詩：アク・モーン_警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"警告","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508484,"overlayVOffset":2.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"アク・モーン","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":10.0,"Match":"(12809>40239)","MatchDelay":30.5}]}
~Lv2~{"Name":"P4_光と闇の竜詩：アク・モーン_CD10","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"10","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"10","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":30.5}]}
~Lv2~{"Name":"P4_光と闇の竜詩：アク・モーン_CD9","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"9","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"9","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":31.5}]}
~Lv2~{"Name":"P4_光と闇の竜詩：アク・モーン_CD8","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"8","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"8","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":32.5}]}
~Lv2~{"Name":"P4_光と闇の竜詩：アク・モーン_CD7","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"7","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"7","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":33.5}]}
~Lv2~{"Name":"P4_光と闇の竜詩：アク・モーン_CD6","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"6","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"6","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":34.5}]}
~Lv2~{"Name":"P4_光と闇の竜詩：アク・モーン_CD5","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"5","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":35.5}]}
~Lv2~{"Name":"P4_光と闇の竜詩：アク・モーン_CD4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":36.5}]}
~Lv2~{"Name":"P4_光と闇の竜詩：アク・モーン_CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":37.5}]}
~Lv2~{"Name":"P4_光と闇の竜詩：アク・モーン_CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508731,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":38.5}]}
~Lv2~{"Name":"P4_光と闇の竜詩：アク・モーン_CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40239)","MatchDelay":39.5}]}
~Lv2~{"Name":"P4_時間結晶：砂時計線","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"砂時計(quick)","type":3,"refY":11.0,"offY":-9.5,"radius":0.0,"color":3355506687,"thicc":16.0,"refActorPlaceholder":[],"refActorNPCNameID":9823,"refActorComparisonAnd":true,"refActorComparisonType":7,"includeRotation":true,"refActorVFXPath":"vfx/channeling/eff/chn_d1049_quick01k1.avfx","refActorVFXMax":13000,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"砂時計(slow)","type":3,"refY":11.0,"offY":-9.5,"radius":0.0,"color":3372155045,"thicc":16.0,"refActorPlaceholder":[],"refActorNPCNameID":9823,"refActorComparisonAnd":true,"refActorComparisonType":7,"includeRotation":true,"refActorVFXPath":"vfx/channeling/eff/chn_d1049_slow01k1.avfx","refActorVFXMax":13000,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":13.0,"Match":"(12809>40240)","MatchDelay":9.7}]}
~Lv2~{"Name":"P4_時間結晶：開幕デバフ","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"赤ブリ","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3371826944,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"赤ブリ","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[3263,2462],"refActorRequireAllBuffs":true,"refActorComparisonType":5,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"赤ロガ","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508484,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"エアロガ","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[2463,3263],"refActorRequireAllBuffs":true,"refActorComparisonType":5,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"エラプション","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3372024063,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"エラプ","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[3264,2460],"refActorRequireAllBuffs":true,"refActorComparisonType":5,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"青ホーリー","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3372177408,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"その他","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[3264,2454],"refActorRequireAllBuffs":true,"refActorComparisonType":5,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"青ウォタガ","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3372177408,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"その他","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[3264,2461],"refActorRequireAllBuffs":true,"refActorComparisonType":5,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"青ブリ","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3372177408,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"その他","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorRequireBuff":true,"refActorBuffId":[3264,2462],"refActorRequireAllBuffs":true,"refActorComparisonType":5,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":13.0,"Match":"(12809>40240)","MatchDelay":9.7}]}
~Lv2~{"Name":"P4_時間結晶：光の大波","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"光の大波","type":3,"refY":10.0,"radius":20.0,"fillIntensity":0.345,"thicc":4.0,"refActorNPCNameID":12809,"refActorRequireCast":true,"refActorCastId":[40253],"refActorUseCastTime":true,"refActorCastTimeMax":1.699,"refActorComparisonType":6,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
~Lv2~{"Name":"P4_時間結晶：爪解除","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"爪解除","type":1,"radius":1.0,"color":4292935424,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3370581760,"thicc":4.0,"overlayText":"デバフ解除","refActorDataID":2014529,"refActorComparisonType":3,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
~Lv2~{"Name":"P4_時間結晶：聖竜_ヒットボックス","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"当たり判定_矢印1","type":3,"refY":0.5,"offY":-0.5,"radius":0.0,"color":3372154890,"fillIntensity":0.345,"thicc":5.0,"refActorModelID":1602,"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"当たり判定_矢印2","type":3,"refY":0.5,"offX":-0.5,"radius":0.0,"color":3372154890,"fillIntensity":0.345,"thicc":5.0,"refActorModelID":1602,"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"当たり判定_矢印3","type":3,"refY":0.5,"offX":0.5,"radius":0.0,"color":3372154890,"fillIntensity":0.345,"thicc":5.0,"refActorModelID":1602,"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"当たり判定_サークル","type":1,"radius":1.0,"color":3372154890,"Filled":false,"fillIntensity":0.345,"thicc":4.0,"refActorModelID":1602,"refActorComparisonType":1,"includeRotation":true,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}]}
~Lv2~{"Name":"P4_時間結晶：悲しみの砂時計_メイルシュトローム1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"サークル","type":1,"radius":12.0,"color":4278190335,"fillIntensity":0.304,"thicc":4.0,"refActorNPCNameID":9823,"refActorComparisonType":6,"refActorTether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":10.0,"refActorTetherParam2":134,"refActorTetherConnectedWithPlayer":[]}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40240)","MatchDelay":15.0}],"Freezing":true,"FreezeFor":6.5}
~Lv2~{"Name":"P4_時間結晶：悲しみの砂時計_メイルシュトローム2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"サークル","type":1,"radius":12.0,"color":4278190335,"fillIntensity":0.3,"thicc":4.0,"refActorNPCNameID":9823,"refActorComparisonType":6,"refActorTether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":10.0,"refActorIsTetherInvert":true,"refActorTetherConnectedWithPlayer":[]}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40240)","MatchDelay":15.0}],"Freezing":true,"FreezeFor":13.0,"FreezeDisplayDelay":6.5}
~Lv2~{"Name":"P4_時間結晶：悲しみの砂時計_メイルシュトローム3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"ディレイ_サークル","type":1,"radius":12.0,"color":4278190335,"fillIntensity":0.3,"thicc":4.0,"refActorNPCNameID":9823,"refActorComparisonType":6,"refActorTether":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":1000.0,"refActorTetherParam2":133,"refActorTetherConnectedWithPlayer":[]}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(12809>40240)","MatchDelay":15.0}],"Freezing":true,"FreezeFor":19.0,"FreezeDisplayDelay":13.0}
~Lv2~{"Name":"P4_時間結晶：デバフ解除_マーカー処理_1234->B23D","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"Attack1","type":1,"radius":1.0,"Donut":0.5,"color":3355505151,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355505151,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":6.0,"overlayText":"B","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refMark":true},{"Name":"Attack2","type":1,"radius":1.0,"Donut":0.5,"color":3355476735,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355476735,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":6.0,"overlayText":"2","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refMark":true,"refMarkID":1},{"Name":"Attack3","type":1,"radius":1.0,"Donut":0.5,"color":3370581760,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3370581760,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":6.0,"overlayText":"3","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refMark":true,"refMarkID":2},{"Name":"Attack4","type":1,"radius":1.0,"Donut":0.5,"color":3372024063,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3372024063,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":6.0,"overlayText":"D","refActorPlaceholder":["<1>","<2>","<3>","<4>","<5>","<6>","<7>","<8>"],"refActorComparisonType":5,"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"refMark":true,"refMarkID":3},{"Name":"青デバフ","type":1,"radius":0.0,"Filled":false,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3369795328,"overlayVOffset":2.0,"overlayFScale":2.0,"thicc":0.0,"overlayText":"デバフ解除","refActorRequireBuff":true,"refActorBuffId":[3264],"refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"1(B)誘導ライン","type":2,"refX":110.0,"refY":100.0,"refZ":-9.536743E-07,"offX":99.99538,"offY":85.00135,"offZ":9.536743E-07,"radius":0.0,"color":3355505151,"Filled":false,"fillIntensity":0.5,"thicc":8.0,"overlayText":"B","refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"2誘導ライン","type":2,"refX":107.06421,"refY":107.10308,"refZ":-1.9073486E-06,"offX":100.0,"offY":85.0,"radius":0.0,"color":3355476735,"Filled":false,"fillIntensity":0.345,"thicc":8.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"3誘導ライン","type":2,"refX":92.90527,"refY":107.02756,"refZ":3.8146973E-06,"offX":100.0,"offY":85.0,"radius":0.0,"color":3370581760,"Filled":false,"fillIntensity":0.345,"thicc":8.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"4(D)誘導ライン","type":2,"refX":90.0,"refY":100.0,"offX":100.0,"offY":85.0,"radius":0.0,"color":3372024063,"Filled":false,"fillIntensity":0.345,"thicc":8.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":7.0,"Match":"(12809>40240)","MatchDelay":29.7}]}
~Lv2~{"Name":"P5_光塵の剣：エクサ基準点","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"北","refX":100.0,"refY":93.0,"radius":0.2,"color":3355506687,"fillIntensity":1.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南","refX":100.0,"refY":107.0,"radius":0.2,"color":3355506687,"fillIntensity":1.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"西","refX":93.0,"refY":100.0,"radius":0.2,"color":3355506687,"fillIntensity":1.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"東","refX":107.0,"refY":100.0,"radius":0.2,"color":3355506687,"fillIntensity":1.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"南北Line","type":2,"Enabled":false,"refX":100.0,"refY":112.0,"offX":100.0,"offY":88.0,"offZ":-1.9073486E-06,"radius":0.0,"color":3355506687,"fillIntensity":0.345,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"東西Line","type":2,"Enabled":false,"refX":88.0,"refY":100.0,"offX":112.0,"offY":100.0,"radius":0.0,"color":3355506687,"fillIntensity":0.345,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Line1","type":2,"Enabled":false,"refX":105.0,"refY":88.0,"refZ":3.8146973E-06,"offX":88.0,"offY":105.0,"radius":0.0,"color":3355506687,"fillIntensity":0.345,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Line2","type":2,"Enabled":false,"refX":88.0,"refY":95.0,"offX":105.0,"offY":112.0,"offZ":-9.536743E-07,"radius":0.0,"color":3355506687,"fillIntensity":0.345,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Line3","type":2,"Enabled":false,"refX":95.0,"refY":112.0,"refZ":1.9073486E-06,"offX":112.0,"offY":95.0,"radius":0.0,"color":3355506687,"fillIntensity":0.345,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"Line4","type":2,"Enabled":false,"refX":95.0,"refY":88.0,"offX":112.0,"offY":105.0,"radius":0.0,"color":3355506687,"fillIntensity":0.345,"thicc":4.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":30.0,"Match":"(13561>40306)"}]}
~Lv2~{"Name":"P5_光塵の剣：0","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"1","type":3,"refY":-5.0,"offY":5.0,"radius":40.0,"color":4278190335,"fillIntensity":0.304,"thicc":4.0,"refActorDataID":9020,"refActorRequireCast":true,"refActorCastId":[40307],"refActorUseCastTime":true,"refActorCastTimeMin":3.0,"refActorCastTimeMax":10.0,"refActorComparisonType":3,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"2","type":3,"refY":5.0,"offY":-5.0,"radius":40.42,"color":4278190335,"fillIntensity":0.304,"thicc":4.0,"refActorDataID":9020,"refActorRequireCast":true,"refActorCastId":[40307],"refActorUseCastTime":true,"refActorCastTimeMin":3.0,"refActorCastTimeMax":10.0,"refActorComparisonType":3,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"FreezeDisplayDelay":6.7}
~Lv2~{"Name":"P5_光塵の剣：1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"1","type":3,"refY":10.0,"offY":5.0,"radius":40.0,"color":4278255407,"fillIntensity":0.3,"thicc":4.0,"refActorDataID":9020,"refActorRequireCast":true,"refActorCastId":[40307],"refActorUseCastTime":true,"refActorCastTimeMax":0.5,"refActorComparisonType":3,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"2","type":3,"refY":-10.0,"offY":-5.0,"radius":40.42,"color":4294911744,"fillIntensity":0.305,"thicc":4.0,"refActorDataID":9020,"refActorRequireCast":true,"refActorCastId":[40307],"refActorUseCastTime":true,"refActorCastTimeMax":0.5,"refActorComparisonType":3,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"Freezing":true,"FreezeFor":8.7,"IntervalBetweenFreezes":0.8,"FreezeDisplayDelay":6.7}
~Lv2~{"Name":"P5_光塵の剣：2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"1","type":3,"refY":15.0,"offY":10.0,"radius":40.0,"color":4278190335,"fillIntensity":0.3,"refActorDataID":9020,"refActorRequireCast":true,"refActorCastId":[40307],"refActorUseCastTime":true,"refActorCastTimeMax":0.5,"refActorComparisonType":3,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"2","type":3,"refY":-15.0,"offY":-10.0,"radius":40.42,"color":4278190335,"fillIntensity":0.3,"refActorDataID":9020,"refActorRequireCast":true,"refActorCastId":[40307],"refActorUseCastTime":true,"refActorCastTimeMax":0.5,"refActorComparisonType":3,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"Freezing":true,"FreezeFor":10.7,"IntervalBetweenFreezes":0.8,"FreezeDisplayDelay":8.7}
~Lv2~{"Name":"P5_光塵の剣：3 ","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"1","type":3,"refY":20.0,"offY":15.0,"radius":40.0,"color":4278190335,"fillIntensity":0.3,"refActorDataID":9020,"refActorRequireCast":true,"refActorCastId":[40307],"refActorUseCastTime":true,"refActorCastTimeMax":0.5,"refActorComparisonType":3,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"2","type":3,"refY":-20.0,"offY":-15.0,"radius":40.42,"color":4278190335,"fillIntensity":0.3,"refActorDataID":9020,"refActorRequireCast":true,"refActorCastId":[40307],"refActorUseCastTime":true,"refActorCastTimeMax":0.5,"refActorComparisonType":3,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"Freezing":true,"FreezeFor":12.7,"IntervalBetweenFreezes":0.8,"FreezeDisplayDelay":10.7}
~Lv2~{"Name":"P5_光塵の剣：4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"1","type":3,"refY":25.0,"offY":20.0,"radius":40.0,"color":4278190335,"fillIntensity":0.3,"refActorDataID":9020,"refActorRequireCast":true,"refActorCastId":[40307],"refActorUseCastTime":true,"refActorCastTimeMax":0.5,"refActorComparisonType":3,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"2","type":3,"refY":-25.0,"offY":-20.0,"radius":40.42,"color":4278190335,"fillIntensity":0.3,"refActorDataID":9020,"refActorRequireCast":true,"refActorCastId":[40307],"refActorUseCastTime":true,"refActorCastTimeMax":0.5,"refActorComparisonType":3,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"Freezing":true,"FreezeFor":14.7,"IntervalBetweenFreezes":0.8,"FreezeDisplayDelay":12.7}
~Lv2~{"Name":"P5_光塵の剣：5 ","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"1","type":3,"refY":30.0,"offY":25.0,"radius":40.0,"color":4278190335,"fillIntensity":0.3,"refActorDataID":9020,"refActorRequireCast":true,"refActorCastId":[40307],"refActorUseCastTime":true,"refActorCastTimeMax":0.5,"refActorComparisonType":3,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"2","type":3,"refY":-30.0,"offY":-25.0,"radius":40.42,"color":4278190335,"fillIntensity":0.3,"refActorDataID":9020,"refActorRequireCast":true,"refActorCastId":[40307],"refActorUseCastTime":true,"refActorCastTimeMax":0.5,"refActorComparisonType":3,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"Freezing":true,"FreezeFor":16.7,"IntervalBetweenFreezes":0.8,"FreezeDisplayDelay":14.7}
~Lv2~{"Name":"P5_光塵の剣：6","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"1","type":3,"refY":35.0,"offY":30.0,"radius":40.0,"color":4278190335,"fillIntensity":0.3,"refActorDataID":9020,"refActorRequireCast":true,"refActorCastId":[40307],"refActorUseCastTime":true,"refActorCastTimeMax":0.5,"refActorComparisonType":3,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"2","type":3,"refY":-35.0,"offY":-30.0,"radius":40.42,"color":4278190335,"fillIntensity":0.3,"refActorDataID":9020,"refActorRequireCast":true,"refActorCastId":[40307],"refActorUseCastTime":true,"refActorCastTimeMax":0.5,"refActorComparisonType":3,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"Freezing":true,"FreezeFor":18.7,"IntervalBetweenFreezes":0.8,"FreezeDisplayDelay":16.7}
~Lv2~{"Name":"P5_光塵の剣：7","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"ElementsL":[{"Name":"1","type":3,"refY":40.0,"offY":35.0,"radius":40.0,"color":4278190335,"fillIntensity":0.3,"refActorDataID":9020,"refActorRequireCast":true,"refActorCastId":[40307],"refActorUseCastTime":true,"refActorCastTimeMax":0.5,"refActorComparisonType":3,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"2","type":3,"refY":-40.0,"offY":-35.0,"radius":40.42,"color":4278190335,"fillIntensity":0.3,"refActorDataID":9020,"refActorRequireCast":true,"refActorCastId":[40307],"refActorUseCastTime":true,"refActorCastTimeMax":0.5,"refActorComparisonType":3,"includeRotation":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"Freezing":true,"FreezeFor":20.7,"IntervalBetweenFreezes":0.8,"FreezeDisplayDelay":18.7}
~Lv2~{"Name":"P5_アク・モーン：4-4Stack","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"H1","type":1,"radius":3.8,"Donut":0.2,"color":4293721856,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278255364,"thicc":4.0,"overlayText":"H1","refActorPlaceholder":["<h1>"],"refActorComparisonType":5,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0},{"Name":"H2","type":1,"radius":3.8,"Donut":0.2,"color":4293721856,"Filled":false,"fillIntensity":0.3,"overlayBGColor":3355443200,"overlayTextColor":4278255364,"thicc":5.0,"overlayText":"H2","refActorPlaceholder":["<h2>"],"refActorComparisonType":5,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":6.0,"Match":"(13561>40310)","MatchDelay":2.0}]}
~Lv2~{"Name":"P5_アク・モーン：警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"警告","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508484,"overlayVOffset":2.0,"overlayFScale":2.5,"thicc":0.0,"overlayText":"アク・モーン","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":15.0,"Match":"(13561>40306)","MatchDelay":17.7}]}
~Lv2~{"Name":"P5_アク・モーン：CD15","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"15","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"15","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40306)","MatchDelay":17.7}]}
~Lv2~{"Name":"P5_アク・モーン：CD14","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"14","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"14","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40306)","MatchDelay":18.7}]}
~Lv2~{"Name":"P5_アク・モーン：CD13","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"13","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"13","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40306)","MatchDelay":19.7}]}
~Lv2~{"Name":"P5_アク・モーン：CD12","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"12","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"12","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40306)","MatchDelay":20.7}]}
~Lv2~{"Name":"P5_アク・モーン：CD11","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"11","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"11","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40306)","MatchDelay":21.7}]}
~Lv2~{"Name":"P5_アク・モーン：CD10","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"10","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"10","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40306)","MatchDelay":22.7}]}
~Lv2~{"Name":"P5_アク・モーン：CD9","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"9","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"9","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40306)","MatchDelay":23.7}]}
~Lv2~{"Name":"P5_アク・モーン：CD8","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"8","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"8","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40306)","MatchDelay":24.7}]}
~Lv2~{"Name":"P5_アク・モーン：CD7","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"7","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"7","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40306)","MatchDelay":25.7}]}
~Lv2~{"Name":"P5_アク・モーン：CD6","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"6","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"6","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40306)","MatchDelay":26.7}]}
~Lv2~{"Name":"P5_アク・モーン：CD5","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"5","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40306)","MatchDelay":27.7}]}
~Lv2~{"Name":"P5_アク・モーン：CD4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40306)","MatchDelay":28.7}]}
~Lv2~{"Name":"P5_アク・モーン：CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40306)","MatchDelay":29.7}]}
~Lv2~{"Name":"P5_アク・モーン：CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508694,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40306)","MatchDelay":30.7}]}
~Lv2~{"Name":"P5_アク・モーン：CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40306)","MatchDelay":31.7}]}
~Lv2~{"Name":"P5_パラダイスリゲインド：初期位置","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"中心","refX":100.0,"refY":100.0,"radius":2.5,"color":3356032768,"Filled":false,"fillIntensity":0.5,"thicc":8.0,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":6.7,"Match":"(13561>40319)"}]}
~Lv2~{"Name":"P5_光と闇の片翼：1回目_光","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"Light","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508731,"overlayVOffset":3.0,"overlayFScale":5.0,"thicc":0.0,"overlayText":"Light","refActorNPCNameID":13561,"refActorComparisonType":6,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":6.7,"Match":"(13561>40313)"}]}
~Lv2~{"Name":"P5_光と闇の片翼：1回目_闇","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"Dark","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3372155119,"overlayVOffset":3.0,"overlayFScale":5.0,"thicc":0.0,"overlayText":"Dark","refActorNPCNameID":13561,"refActorComparisonType":6,"onlyVisible":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":6.7,"Match":"(13561>40233)"}]}
~Lv2~{"Name":"P5_光と闇の片翼(1回目)：警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"警告","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508484,"overlayVOffset":2.0,"overlayFScale":2.5,"thicc":0.0,"overlayText":"片翼1回目","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":11.7,"Match":"(13561>40319)","MatchDelay":4.0}]}
~Lv2~{"Name":"P5_光と闇の片翼(1回目)：CD12","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"12","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"12","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":0.7,"Match":"(13561>40319)","MatchDelay":4.0}]}
~Lv2~{"Name":"P5_光と闇の片翼(1回目)：CD11","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"11","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"11","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40319)","MatchDelay":4.7}]}
~Lv2~{"Name":"P5_光と闇の片翼(1回目)：CD10","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"10","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"10","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40319)","MatchDelay":5.7}]}
~Lv2~{"Name":"P5_光と闇の片翼(1回目)：CD9","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"9","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"9","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40319)","MatchDelay":6.7}]}
~Lv2~{"Name":"P5_光と闇の片翼(1回目)：CD8","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"8","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508731,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"8","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40319)","MatchDelay":7.7}]}
~Lv2~{"Name":"P5_光と闇の片翼(1回目)：CD7","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"7","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508731,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"7","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40319)","MatchDelay":8.7}]}
~Lv2~{"Name":"P5_光と闇の片翼(1回目)：CD6","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"6","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508223,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"6","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40319)","MatchDelay":9.7}]}
~Lv2~{"Name":"P5_光と闇の片翼(1回目)：CD5","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"5","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508731,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"5","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40319)","MatchDelay":10.7}]}
~Lv2~{"Name":"P5_光と闇の片翼(1回目)：CD4","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"4","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508725,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"4","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40319)","MatchDelay":11.7}]}
~Lv2~{"Name":"P5_光と闇の片翼(1回目)：CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40319)","MatchDelay":12.7}]}
~Lv2~{"Name":"P5_光と闇の片翼(1回目)：CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40319)","MatchDelay":13.7}]}
~Lv2~{"Name":"P5_光と闇の片翼(1回目)：CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40319)","MatchDelay":14.7}]}
~Lv2~{"Name":"P5_光と闇の片翼(2回目)：警告","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"警告","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508484,"overlayVOffset":2.0,"overlayFScale":2.5,"thicc":0.0,"overlayText":"片翼2回目","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":3.3,"Match":"(13561>40319)","MatchDelay":15.7}]}
~Lv2~{"Name":"P5_光と闇の片翼(2回目)：CD3","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"3","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508570,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"3","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.3,"Match":"(13561>40319)","MatchDelay":15.7}]}
~Lv2~{"Name":"P5_光と闇の片翼(2回目)：CD2","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"2","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355508731,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"2","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40319)","MatchDelay":17.0}]}
~Lv2~{"Name":"P5_光と闇の片翼(2回目)：CD1","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"1","type":1,"radius":0.0,"fillIntensity":0.5,"overlayBGColor":3355443200,"overlayTextColor":3355443455,"overlayVOffset":3.0,"overlayFScale":3.0,"thicc":0.0,"overlayText":"1","refActorType":1,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":1.0,"Match":"(13561>40319)","MatchDelay":18.0}]}
~Lv2~{"Name":"P5_星霊の剣：4-4Stack","Group":"Ultimate Futures Rewritten","ZoneLockH":[1238],"DCond":5,"ElementsL":[{"Name":"T1","type":3,"refY":50.0,"radius":3.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"thicc":4.0,"refActorNPCNameID":13561,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"faceplayer":"<t1>","FillStep":2.0},{"Name":"T2","type":3,"refY":50.0,"radius":3.0,"color":4278190335,"Filled":false,"fillIntensity":0.3,"thicc":4.0,"refActorNPCNameID":13561,"refActorComparisonType":6,"includeRotation":true,"onlyVisible":true,"FaceMe":true,"refActorTetherTimeMin":0.0,"refActorTetherTimeMax":0.0,"faceplayer":"<t2>","FillStep":2.0}],"UseTriggers":true,"Triggers":[{"Type":2,"Duration":20.0,"Match":"(13561>40316)"}]}
```