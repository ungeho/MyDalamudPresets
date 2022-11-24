* General

  全てのジョブで戦闘時のみ表示

  * HitBox

    自身を中心にdotを表示（当たり判定の目安)

  * 6mAoE

    自身を中心に6mの円を表示（ほとんどの散開用のAoEのサイズは半径6m）

```
~Lv2~{"Name":"General","Group":"Buttle Jobs","DCond":1,"ElementsL":[{"Name":"HitBox","type":1,"radius":0.0,"color":3355508515,"thicc":2.2,"refActorType":1},{"Name":"6mAoE","type":1,"radius":6.0,"color":3355508515,"thicc":0.2,"refActorType":1}]}
```

* SCH

  学者でインスタンス内でのみ表示（アライアンスレイド、ボズヤ、エウレカなどで学者をする時はチェックを外す事を推奨）

  フェアリー・エオスとフェアリー・セレネを中心に半径15mの円を表示（妖精のヒールレンジの目安）

```
~Lv2~{"Name":"SCH","Group":"Buttle Jobs","DCond":2,"ElementsL":[{"Name":"Fairy1","type":1,"radius":15.0,"refActorNPCNameID":1398,"refActorComparisonType":6},{"Name":"Fairy2","type":1,"radius":15.0,"refActorNPCNameID":1399,"refActorComparisonType":6}],"JobLock":268435456}
```

* Heal Range

  幻術士、白魔導士、学者、占星術士、賢者で戦闘時のみ表示

  自身を中心に半径15mと半径20mの円を表示（ヒールレンジの目安）

```
~Lv2~{"Name":"Heal Range","Group":"Buttle Jobs","DCond":1,"ElementsL":[{"Name":"15m Heal","type":1,"radius":15.0,"color":3372156928,"thicc":1.0,"refActorType":1},{"Name":"20m Heal","type":1,"radius":20.0,"color":3369350365,"thicc":1.0,"refActorType":1}],"JobLock":1108386775104}
```

* Dancer

  踊り子で戦闘時のみ表示

  自身を中心に半径5mと半径15mの円を表示（範囲攻撃とステップの範囲の目安）

```
~Lv2~{"Name":"Dancer","Group":"Buttle Jobs","DCond":1,"ElementsL":[{"Name":"AoE 5m","type":1,"radius":5.0,"color":3369912860,"thicc":1.0,"refActorType":1},{"Name":"AoE 15m","type":1,"radius":15.0,"color":3370858732,"refActorType":1}],"JobLock":274877906944}
```

* Target Circle

  全てのジョブで常時表示

  ターゲット対象した対象のターゲットサークルの半径（HITBOXR）を表示する。

  特に、ほとんどの散開用AoE（半径6mの円型）は、ターゲットサークルの半径（HITBOXR）が8（7.9）以上の場合、ターゲットサークル上で8方向散開が可能

```
{"Name":"Target Circle Range","type":1,"radius":0.0,"overlayVOffset":1.0,"overlayPlaceholders":true,"overlayText":"HITBOXR->$HITBOXR","refActorType":2}
```

* All Players（動作未確認）

  初期設定オフにしているので使う場合はチェックを入れる

  Generalと被るので使う場合はGeneralをオフにする事を推奨

  特に、アライアンスレイド、エウレカ、ボズヤなどの人の多いコンテンツではごちゃごちゃするので注意

  全てのジョブで戦闘時かつインスタンス内でのみ表示

  * All hitbox

    全てのプレイヤーキャラクターの中心に点を表示

  * All 6m AoE

    全てのプレイヤーキャラクターを中心に半径6mの円を表示

```
~Lv2~{"Enabled":false,"Name":"All Players","Group":"Buttle Jobs","DCond":3,"ElementsL":[{"Name":"All hitbox","type":1,"radius":0.0,"color":3357277952,"thicc":2.2,"refActorComparisonType":4},{"Name":"All 6m Aoe","type":1,"radius":6.0,"color":3357277952,"thicc":0.2,"refActorComparisonType":4}]}
```
