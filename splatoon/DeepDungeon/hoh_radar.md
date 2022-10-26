***Heaven on High Radar***

  全ての階で表示される。

  表示範囲は0-100m

* Target Sensing Range

  ターゲット対象を中心に、緑色の半径 target hitbox + 10m　の円を表示

  ターゲットしている敵の感知範囲の簡易的な目安

* Turning Enemies

  ターゲット対象を中心に、水色の半径 1.5m の円を表示

  ターゲット対象と自身を繋げる直線を表示（tether）

  敵回しの目安(tetherのチェックはお好み)

* Metastasis Tracker

  自身と転移の石塔を結ぶ赤色の直線を表示

  転移の石塔を中心に、赤色の半径 2m の円を表示

  転移の石塔の場所の目安

  always pixel perfect（足元のdot）が表示された円に入ると転移が開始される

* Treasure Chest Gold

  金箱を中心に、白色の半径 target hitbox + 4m の円を表示

  金箱の場所の目安

  always pixel perfect（足元のdot）が表示された円に入ると取得可能

* Treasure Chest Silver

  銀箱を中心に、青色の半径 target hitbox + 4m の円を表示

  銀箱の場所の目安

  always pixel perfect（足元のdot）が表示された円に入ると取得可能

* Treasure Chest Bronze(2~15)

  銅箱を中心に、橙色の半径 target hitbox + 2.5m の円を表示

  銅箱の場所の目安

  always pixel perfect（足元のdot）が表示された円に入ると取得可能（正確でない）

* Treasure Chest Bronze1(MiMic)

  銅箱(ミミック)を中心に、水色の半径 target hitbox + 2.5m の円を表示

  銅箱（ミミック）の場所の目安

  always pixel perfect（足元のdot）が表示された円に入ると取得可能（正確でない）

* Morphological change Range

  自身を中心に、水色の半径 your hitbox + 18m の円を表示

  形態変化のレンジの目安（敵のターゲットサークルが円に触れていれば有効）

* stepped on the trap

  トラップを踏んだ跡地を中心に、紫色の半径 0.5m の円を表示

  （銀箱を開けた跡地にも表示される）

* always pixel perfect

  自身を中心に、緑色の半径 0m の円を表示（ドット）

  hitboxの目安として常に表示される

* Contact Range

  自身を中心に、赤色の your hitbox + 0.55m の円を表示

  円が敵の中心に触れたら接触判定として感知されてもおかしくはないくらいの目安（あまり正確ではない）

  接触扱いで感知される範囲は、ターゲットサークルの大きさに関わらず敵によってバラバラ

* Trap Hitbox

  自身を中心に、緑色の your hitbox + 1.2m の円を表示

  罠に接触するかの目安

  表示された円が、Deep Dungeon Helper プラグインによって表示された点に触れると、その場所に罠が存在した場合は起爆する。

  同様に、財宝感知で発見した財宝も、Trap Hitboxで表示された円の範囲内に入れる事で掘り起こす事が出来る。

* Mimic Sensing Range(1-29F)

  銅箱から出現するうごめく宝箱を対象に、黄色の　target hitbox + 10m の円を表示

* Mimic Sensing Range(31-60F)

  銀箱から出現するうごめく宝箱を対象に、黄色の　target hitbox + 10m の円を表示

* Mimic Sensing Range(61-99F)

  金箱から出現するうごめく宝箱を対象に、黄色の　target hitbox + 14m の円を表示

  （感知範囲を確認していない為、死者のミミックを参考に14mに設定）

* Mancragora Sensing Range

  敵変化で出現するコリガンを対象に、水色の　target hitbox + 10m の円を表示

* Turning Enemies (Right/Left) point

  ターゲット対象を中心とした半径1.5mの円周上の角度（0/180）度（敵の真横）の位置に水色の半径 0.1m の円を塗りつぶしで表示

  敵回しの目安  

* Turning Enemies Forward

  ターゲット対象を中心に、水色で前方90度の扇範囲を描画

```
~Lv2~{"Name":"Heaven on High Radar","Group":"Heaven on High","ZoneLockH":[775,774,773,772,771,770,782,783,784,785],"ElementsL":[{"Name":"Target Sensing Range","type":1,"radius":10.0,"color":3355508512,"refActorType":2,"includeHitbox":true},{"Name":"Turning Enemies","type":1,"radius":1.5,"color":3372023552,"refActorType":2,"tether":true},{"Name":"Metastasis Tracker","type":1,"radius":2.0,"overlayText":"転移の石塔","refActorDataID":2009507,"refActorComparisonType":3,"includeRotation":true,"tether":true},{"Name":"Treasure Chest Gold","type":1,"radius":4.0,"color":3372220415,"overlayText":"Gold Chest","refActorDataID":2007358,"refActorComparisonType":3,"includeHitbox":true},{"Name":"Treasure Chest Silver","type":1,"radius":4.0,"color":3372158464,"overlayText":"Silver Chest","refActorDataID":2007357,"refActorComparisonType":3,"includeHitbox":true},{"Name":"Treasure Chest Bronze1(Mimic)","type":1,"radius":2.5,"color":3371433728,"overlayText":"Mimic","refActorDataID":2006020,"refActorComparisonType":3,"includeHitbox":true},{"Name":"Treasure Chest Bronze2","type":1,"radius":2.5,"color":3355496447,"overlayText":"Bronze Chest","refActorDataID":1039,"refActorComparisonType":3,"includeHitbox":true},{"Name":"Treasure Chest Bronze3","type":1,"radius":2.5,"color":3355496447,"overlayText":"Bronze Chest","refActorDataID":1040,"refActorComparisonType":3,"includeHitbox":true},{"Name":"Treasure Chest Bronze4","type":1,"radius":2.5,"color":3355496447,"overlayText":"Bronze Chest","refActorDataID":1041,"refActorComparisonType":3,"includeHitbox":true},{"Name":"Treasure Chest Bronze5","type":1,"radius":2.5,"color":3355496447,"overlayText":"Bronze Chest","refActorDataID":1042,"refActorComparisonType":3,"includeHitbox":true},{"Name":"Treasure Chest Bronze6","type":1,"radius":2.5,"color":3355496447,"overlayText":"Bronze Chest","refActorDataID":1043,"refActorComparisonType":3,"includeHitbox":true},{"Name":"Treasure Chest Bronze7","type":1,"radius":2.5,"color":3355496447,"overlayText":"Bronze Chest","refActorDataID":1044,"refActorComparisonType":3,"includeHitbox":true},{"Name":"Treasure Chest Bronze8","type":1,"radius":2.5,"color":3355496447,"overlayText":"Bronze Chest","refActorDataID":1045,"refActorComparisonType":3,"includeHitbox":true},{"Name":"Treasure Chest Bronze9","type":1,"radius":2.5,"color":3355496447,"overlayText":"Bronze Chest","refActorDataID":1047,"refActorComparisonType":3,"includeHitbox":true},{"Name":"Treasure Chest Bronze10","type":1,"radius":2.5,"color":3355496447,"overlayText":"Bronze Chest","refActorDataID":1048,"refActorComparisonType":3,"includeHitbox":true},{"Name":"Treasure Chest Bronze11","type":1,"radius":2.5,"color":3355496447,"overlayText":"Bronze Chest","refActorDataID":1049,"refActorComparisonType":3,"includeHitbox":true},{"Name":"Treasure Chest Bronze12","type":1,"radius":2.5,"color":3355496447,"overlayText":"Bronze Chest","refActorDataID":1046,"refActorComparisonType":3,"includeHitbox":true},{"Name":"Treasure Chest Bronze13","type":1,"radius":2.5,"color":3355496447,"overlayText":"Bronze Chest","refActorDataID":1036,"refActorComparisonType":3,"includeHitbox":true},{"Name":"Treasure Chest Bronze14","type":1,"radius":2.5,"color":3355496447,"overlayText":"Bronze Chest","refActorDataID":1037,"refActorComparisonType":3,"includeHitbox":true},{"Name":"Treasure Chest Bronze15","type":1,"radius":2.5,"color":3355496447,"overlayText":"Bronze Chest","refActorDataID":1038,"refActorComparisonType":3,"includeHitbox":true},{"Name":"Morphological change Range","type":1,"radius":18.0,"color":3372217088,"refActorType":1,"includeOwnHitbox":true},{"Name":"stepped on the trap","type":1,"radius":0.5,"color":3372024063,"overlayText":"stepped on the trap","refActorModelID":480,"refActorComparisonType":1},{"Name":"always pixel perfect","type":1,"radius":0.0,"color":3357277952,"refActorType":1},{"Name":"Contact Range","type":1,"radius":0.55,"refActorType":1,"includeOwnHitbox":true},{"Name":"Trap Hitbox","type":1,"radius":1.2,"color":3355508480,"refActorType":1,"includeOwnHitbox":true},{"Name":"Mimic Sensing Range(1-29F)","type":1,"radius":10.0,"color":3355497983,"overlayText":"Mimic(can stun)","refActorNPCID":7392,"refActorComparisonType":4,"includeHitbox":true},{"Name":"Mimic Sensing Range(31-60F)","type":1,"radius":10.0,"color":3355497983,"overlayText":"Mimic(can stun)","refActorNPCID":7393,"refActorComparisonType":4,"includeHitbox":true},{"Name":"Mimic Sensing Range(61-99F)","type":1,"radius":14.0,"color":3355497983,"overlayText":"Mimic","refActorNPCID":7394,"refActorComparisonType":4,"includeHitbox":true},{"Name":"Mandragora Sensing Range","type":1,"radius":10.0,"color":3370974976,"refActorNPCID":7610,"refActorComparisonType":4,"includeHitbox":true},{"Name":"Turning Enemies Right point","type":1,"offX":1.5,"radius":0.1,"color":3372023552,"refActorType":2,"includeRotation":true,"Filled":true},{"Name":"Turning Enemies Left point","type":1,"offX":-1.5,"radius":0.1,"color":3372023552,"refActorType":2,"includeRotation":true,"Filled":true},{"Name":"Turning Enemies Forward","type":4,"radius":10.0,"coneAngleMin":-45,"coneAngleMax":45,"color":3372023552,"FillStep":90.0,"refActorType":2,"includeHitbox":true,"includeRotation":true,"Filled":true}]}
```
