cc.Class({
    name: 'RavenboundCocosGame',
    extends: cc.Component,

    properties: {
        startMode: {
            default: 0,
            tooltip: '0 = 正常开场；1 = 直接第一晚；2 = 直接第二晚；3 = 直接第三晚；4 = 猎魔委托板。'
        },
        musicVolume: {
            default: 0.78,
            range: [0, 1, 0.01],
            tooltip: '背景音乐音量。'
        },
        sfxVolume: {
            default: 0.86,
            range: [0, 1, 0.01],
            tooltip: '音效音量。'
        }
    },

    onLoad: function () {
        this.width = 960;
        this.height = 640;
        this.musicPath = '';
        this.mode = 'boot';
        this.buttons = [];
        this.currentChoices = [];
        this.dialogue = null;
        this.main = null;
        this.run = null;
        this.battle = null;

        this.assets = {
            backgrounds: {
                first: 'art/backgrounds/first_night',
                second: 'art/backgrounds/second_night',
                third: 'art/backgrounds/third_night',
                battle: 'art/backgrounds/battle_stage'
            },
            portraits: {
                hunter: 'art/portraits/hunter',
                triss: 'art/portraits/triss',
                yennefer: 'art/portraits/yennefer'
            },
            monsters: {
                corrupted_wolf: 'art/monsters/corrupted_wolf',
                blood_wraith: 'art/monsters/blood_wraith',
                black_nail_puppet: 'art/monsters/black_nail_puppet',
                black_moon_knight: 'art/monsters/black_moon_knight',
                black_nail_thrall: 'art/monsters/black_nail_puppet',
                black_wax_gate_shade: 'art/monsters/blood_wraith',
                black_wax_acolyte: 'art/monsters/black_nail_puppet',
                crowbone_stitcher: 'art/monsters/black_moon_knight',
                black_wax_saint: 'art/monsters/black_nail_puppet'
            },
            music: {
                first: 'music/first_night',
                second: 'music/second_night',
                third: 'music/third_night',
                battle: 'music/battle'
            },
            sfx: {
                click: 'sfx/ui_click',
                sword: 'sfx/sword_hit',
                fire: 'sfx/fire_cast',
                victory: 'sfx/battle_victory',
                confirm: 'sfx/choice_confirm',
                transition: 'sfx/night_transition'
            }
        };

        this.openingNarration = [
            '黑月升起后的第七个冬天，北境的村庄开始一个接一个沉默。',
            '有人说，是瘟疫。',
            '有人说，是狼群。',
            '但猎魔人知道……真正会吃人的东西，往往披着人的皮。'
        ];

        this.story = this.buildStoryData();
        this.contracts = this.buildContracts();
        this.partners = this.buildPartners();
        this.loadouts = this.buildLoadouts();
        this.ledger = this.loadLedger();
        this.mainEndings = this.loadJson('ravenbound_cocos_main_endings', []);
        this.contractEndings = this.loadJson('ravenbound_cocos_contract_endings', {});
        this.party = this.createStoryParty(false);

        this.buildView();
        this.refreshHeader();
        this.bindKeyboard();

        if (this.startMode >= 1 && this.startMode <= 3) {
            this.startMainNight(this.startMode);
        } else if (this.startMode === 4) {
            this.showContractBoard();
        } else {
            this.showHome();
        }
    },

    onDestroy: function () {
        cc.audioEngine.stopMusic();
        if (cc.systemEvent) {
            cc.systemEvent.off(cc.SystemEvent.EventType.KEY_DOWN, this.onKeyDown, this);
        }
    },

    bindKeyboard: function () {
        if (cc.systemEvent) {
            cc.systemEvent.on(cc.SystemEvent.EventType.KEY_DOWN, this.onKeyDown, this);
        }
    },

    onKeyDown: function (event) {
        if (!this.currentChoices || this.currentChoices.length === 0) {
            return;
        }

        var key = event.keyCode;
        var enter = cc.macro && cc.macro.KEY ? cc.macro.KEY.enter : 13;
        var space = cc.macro && cc.macro.KEY ? cc.macro.KEY.space : 32;
        for (var i = 0; i < this.currentChoices.length; i++) {
            if (key === 49 + i || key === 97 + i) {
                this.choose(i);
                return;
            }
        }

        if ((key === enter || key === space) && this.currentChoices.length === 1) {
            this.choose(0);
        }
    },

    buildView: function () {
        this.node.setContentSize(this.width, this.height);

        this.background = this.createNode('Background', this.node, 0, 0, this.width, this.height);
        this.background.zIndex = -20;
        this.backgroundSprite = this.background.addComponent(cc.Sprite);
        this.backgroundSprite.sizeMode = cc.Sprite.SizeMode.CUSTOM;

        this.tint = this.createPanel('Tint', this.node, 0, 0, this.width, this.height, new cc.Color(0, 0, 0, 105));
        this.tint.zIndex = -10;

        this.titleLabel = this.createLabel('Title', this.node, '', 28, -420, 292, 520, 38, new cc.Color(246, 220, 146, 255), cc.Label.HorizontalAlign.LEFT);
        this.statLabel = this.createLabel('Stats', this.node, '', 16, 185, 292, 520, 30, new cc.Color(222, 215, 188, 255), cc.Label.HorizontalAlign.RIGHT);
        this.questLabel = this.createLabel('Quest', this.node, '', 16, -412, 255, 800, 28, new cc.Color(188, 198, 176, 255), cc.Label.HorizontalAlign.LEFT);

        this.storyPanel = this.createPanel('StoryPanel', this.node, -192, 60, 560, 315, new cc.Color(8, 13, 15, 222));
        this.storyTitle = this.createLabel('StoryTitle', this.storyPanel, '', 22, 0, 124, 510, 32, new cc.Color(255, 226, 144, 255), cc.Label.HorizontalAlign.LEFT);
        this.storyText = this.createLabel('StoryText', this.storyPanel, '', 18, 0, 26, 510, 178, new cc.Color(232, 235, 221, 255), cc.Label.HorizontalAlign.LEFT);
        this.hintLabel = this.createLabel('Hint', this.storyPanel, '', 15, 0, -126, 510, 36, new cc.Color(163, 178, 164, 255), cc.Label.HorizontalAlign.LEFT);

        this.visualPanel = this.createPanel('VisualPanel', this.node, 302, 64, 290, 315, new cc.Color(7, 10, 12, 204));
        this.timelineLabel = this.createLabel('Timeline', this.visualPanel, '', 15, 0, 132, 250, 32, new cc.Color(194, 208, 181, 255), cc.Label.HorizontalAlign.CENTER);
        this.monsterNode = this.createNode('Monster', this.visualPanel, 0, 34, 220, 168);
        this.monsterSprite = this.monsterNode.addComponent(cc.Sprite);
        this.monsterSprite.sizeMode = cc.Sprite.SizeMode.CUSTOM;
        this.monsterName = this.createLabel('MonsterName', this.visualPanel, '', 18, 0, -74, 250, 30, new cc.Color(226, 219, 190, 255), cc.Label.HorizontalAlign.CENTER);
        this.enemyStatLabel = this.createLabel('EnemyStats', this.visualPanel, '', 15, 0, -110, 250, 52, new cc.Color(190, 198, 181, 255), cc.Label.HorizontalAlign.CENTER);

        this.partyRow = this.createNode('PartyRow', this.node, -292, -178, 380, 112);
        this.partySlots = [];
        this.partySlots.push(this.createPortraitSlot('PartySlot0', this.partyRow, -126, 0));
        this.partySlots.push(this.createPortraitSlot('PartySlot1', this.partyRow, 0, 0));
        this.partySlots.push(this.createPortraitSlot('PartySlot2', this.partyRow, 126, 0));
        this.partyStatus = this.createLabel('PartyStatus', this.node, '', 15, -100, -181, 520, 112, new cc.Color(231, 226, 204, 255), cc.Label.HorizontalAlign.LEFT);

        this.buttonLayer = this.createNode('Buttons', this.node, 0, -268, 900, 145);

        this.transitionLayer = this.createPanel('TransitionLayer', this.node, 0, 0, this.width, this.height, new cc.Color(0, 0, 0, 245));
        this.transitionLayer.zIndex = 30;
        this.transitionTitle = this.createLabel('TransitionTitle', this.transitionLayer, '', 34, 0, 36, 620, 50, new cc.Color(247, 226, 164, 255), cc.Label.HorizontalAlign.CENTER);
        this.transitionSubtitle = this.createLabel('TransitionSubtitle', this.transitionLayer, '', 20, 0, -22, 660, 36, new cc.Color(215, 212, 192, 255), cc.Label.HorizontalAlign.CENTER);
        this.transitionLayer.active = false;
    },

    showHome: function () {
        this.mode = 'home';
        this.dialogue = null;
        this.battle = null;
        this.setBackground('first');
        this.playMusic(this.assets.music.first);
        this.party = this.createStoryParty(false);
        this.refreshHeader();
        this.updatePartyView();
        this.setMonsterVisual('black_moon_knight', '黑月阴影');
        this.storyTitle.string = 'Ravenbound Cocos';
        this.storyText.string = 'Cocos 2.4.15 版本已接入主线三晚和猎魔委托短局。\n\n主线是顺序解谜：调查节点、击败伏击、做代价选择，最后进入Boss战。\n短局是Roguelite委托：随机真凶、弱点、村民态度、天气和代价，每局用有限调查推动不同结局。';
        this.hintLabel.string = '选择一个入口开始。';
        this.showChoices([
            { text: '正常开场', action: this.startOpening.bind(this) },
            { text: '直接第一晚', action: this.startMainNight.bind(this, 1) },
            { text: '直接第二晚', action: this.startMainNight.bind(this, 2) },
            { text: '直接第三晚', action: this.startMainNight.bind(this, 3) },
            { text: '猎魔委托板', action: this.showContractBoard.bind(this) },
            { text: '结局图鉴', action: this.showGallery.bind(this) }
        ]);
    },

    startOpening: function () {
        this.mode = 'opening';
        this.openingIndex = 0;
        this.setBackground('first');
        this.playMusic(this.assets.music.first);
        this.showOpeningLine();
    },

    showOpeningLine: function () {
        var line = this.openingNarration[this.openingIndex];
        this.refreshHeader();
        this.questLabel.string = '开场旁白';
        this.timelineLabel.string = '';
        this.monsterName.string = '北境的黑月';
        this.enemyStatLabel.string = '';
        this.storyTitle.string = '黑月开场';
        this.storyText.string = line;
        this.hintLabel.string = '点击继续。';
        this.showChoices([
            {
                text: '继续',
                action: function () {
                    this.openingIndex++;
                    if (this.openingIndex >= this.openingNarration.length) {
                        this.startMainNight(1);
                    } else {
                        this.showOpeningLine();
                    }
                }.bind(this)
            }
        ]);
    },

    startMainNight: function (nightNumber) {
        var night = this.story.nights[nightNumber];
        if (!night) {
            this.showHome();
            return;
        }

        this.mode = 'main';
        this.main = {
            nightNumber: nightNumber,
            stepIndex: 0,
            flags: {},
            choiceIndex: -1,
            truthCorrect: false,
            clues: [],
            night: night
        };
        this.party = this.createStoryParty(nightNumber >= 3);
        this.recoverParty(true);
        this.setBackground(night.background);
        this.playMusic(this.assets.music[night.music]);
        this.updatePartyView();
        this.playNightTransition(night.title, night.subtitle, function () {
            this.showDialogue(night.briefing, function () {
                this.showMainStep();
            }.bind(this), night.subtitle);
        }.bind(this));
    },

    showMainStep: function () {
        if (!this.main) {
            this.showHome();
            return;
        }

        var step = this.main.night.steps[this.main.stepIndex];
        if (!step) {
            this.showHome();
            return;
        }

        this.refreshHeader();
        this.setBackground(this.main.night.background);
        this.playMusic(this.assets.music[this.main.night.music]);
        this.timelineLabel.string = '';
        this.enemyStatLabel.string = this.buildMainKnownFacts();

        if (step.type === 'node') {
            this.setMonsterVisual(step.previewKind || 'black_moon_knight', step.label);
            this.questLabel.string = '当前目标：' + step.objective;
            this.storyTitle.string = step.label;
            this.storyText.string = step.prompt + '\n\n' + this.buildMainKnownFacts();
            this.hintLabel.string = '主线解谜按顺序推进；线索会改变后续战斗参数。';
            this.showChoices([
                { text: '调查' + step.label, action: this.resolveMainNode.bind(this, step) },
                { text: '返回入口', action: this.showHome.bind(this) }
            ]);
            return;
        }

        if (step.type === 'choice') {
            this.showMainTruthChoice(step);
            return;
        }
    },

    resolveMainNode: function (step) {
        if (step.flag) {
            this.main.flags[step.flag] = true;
        }
        if (step.clue) {
            this.main.clues.push(step.clue);
        }

        this.playSfx(this.assets.sfx.confirm);
        this.showDialogue(step.lines, function () {
            if (step.ambush) {
                this.beginMainAmbush(step.ambush);
            } else {
                this.main.stepIndex++;
                this.showMainStep();
            }
        }.bind(this), step.label);
    },

    beginMainAmbush: function (ambush) {
        var encounter = this.createEncounter(ambush.title, [
            this.createEnemies(ambush.kind, ambush.count, ambush.waveBonus || 0, null, ambush.name || ambush.title)
        ], ambush.openingNote || '线索刚落进掌心，黑暗里就有东西靠近。');

        this.startBattle(encounter, function () {
            this.main.stepIndex++;
            this.recoverParty(false);
            this.showMainStep();
        }.bind(this), function () {
            this.showMainDefeat();
        }.bind(this));
    },

    showMainTruthChoice: function (step) {
        this.refreshHeader();
        this.questLabel.string = '当前目标：' + step.objective;
        this.timelineLabel.string = '';
        this.enemyStatLabel.string = this.buildMainKnownFacts();
        this.setMonsterVisual(step.previewKind || 'black_nail_puppet', '真相选择');
        this.storyTitle.string = '审判前的判断';
        this.storyText.string = step.summary + '\n\n' + this.buildMainKnownFacts();
        this.hintLabel.string = '这不是善恶题，是代价题。选完会影响Boss战。';

        this.showChoices([
            { text: step.labels[0], action: this.resolveMainTruthChoice.bind(this, step, 0) },
            { text: step.labels[1], action: this.resolveMainTruthChoice.bind(this, step, 1) },
            { text: step.labels[2], action: this.resolveMainTruthChoice.bind(this, step, 2) }
        ]);
    },

    resolveMainTruthChoice: function (step, index) {
        this.main.choiceIndex = index;
        this.main.truthCorrect = index === step.correctIndex;
        this.playSfx(this.assets.sfx.confirm);
        this.showDialogue(step.choiceLines[index], function () {
            this.beginMainBoss(step);
        }.bind(this), step.labels[index]);
    },

    beginMainBoss: function (step) {
        var modifiers = this.buildMainBossModifiers(this.main.nightNumber, this.main.flags, this.main.choiceIndex, this.main.truthCorrect);
        var boss = this.createEnemies(step.boss.kind, 1, step.boss.waveBonus || 0, modifiers, step.boss.name);
        var encounter = this.createEncounter(step.boss.title, [boss], modifiers.openingNote);
        this.startBattle(encounter, function () {
            this.recoverParty(true);
            this.unlockMainEnding(this.main.nightNumber, this.main.choiceIndex);
            this.showDialogue(this.main.night.settlements[this.main.choiceIndex], function () {
                if (this.main.night.nextNight) {
                    this.startMainNight(this.main.night.nextNight);
                } else {
                    this.showMainComplete();
                }
            }.bind(this), '委托结算');
        }.bind(this), function () {
            this.showMainDefeat();
        }.bind(this));
    },

    showMainDefeat: function () {
        this.mode = 'main_defeat';
        this.refreshHeader();
        this.playMusic(this.assets.music[this.main ? this.main.night.music : 'first']);
        this.storyTitle.string = '委托失败';
        this.storyText.string = '你从夜色里退出来，伤口比答案更先抵达村口。\n\n线索还在，但今晚没有人愿意再开门。';
        this.hintLabel.string = '可以重新挑战当前夜晚。';
        this.showChoices([
            { text: '重试当前夜晚', action: this.startMainNight.bind(this, this.main ? this.main.nightNumber : 1) },
            { text: '返回入口', action: this.showHome.bind(this) }
        ]);
    },

    showMainComplete: function () {
        this.mode = 'main_complete';
        this.refreshHeader();
        this.setBackground('third');
        this.playMusic(this.assets.music.third);
        this.setMonsterVisual('black_wax_saint', '黑蜡余烬');
        this.storyTitle.string = '第三晚结束';
        this.storyText.string = '黑蜡不再流动，灰鸦村也没有真正醒来。\n\n你能收下报酬，也能记住那些没有写进委托书里的名字。';
        this.hintLabel.string = '主线三晚已跑通。';
        this.showChoices([
            { text: '猎魔委托板', action: this.showContractBoard.bind(this) },
            { text: '结局图鉴', action: this.showGallery.bind(this) },
            { text: '返回入口', action: this.showHome.bind(this) }
        ]);
    },

    showDialogue: function (lines, onDone, title) {
        this.mode = 'dialogue';
        this.dialogue = {
            lines: lines || [],
            index: 0,
            onDone: onDone,
            title: title || '对话'
        };
        this.showDialogueLine();
    },

    showDialogueLine: function () {
        if (!this.dialogue || this.dialogue.index >= this.dialogue.lines.length) {
            var done = this.dialogue ? this.dialogue.onDone : null;
            this.dialogue = null;
            if (done) {
                done();
            }
            return;
        }

        var line = this.dialogue.lines[this.dialogue.index];
        this.refreshHeader();
        this.storyTitle.string = this.dialogue.title;
        this.storyText.string = line.speaker + '：\n' + line.text;
        this.hintLabel.string = '点击继续。';
        this.showChoices([
            {
                text: '继续',
                action: function () {
                    this.dialogue.index++;
                    this.showDialogueLine();
                }.bind(this)
            }
        ]);
    },

    showContractBoard: function () {
        this.mode = 'contract_board';
        this.run = null;
        this.battle = null;
        this.setBackground('first');
        this.playMusic(this.assets.music.first);
        this.party = this.createStoryParty(false);
        this.updatePartyView();
        this.refreshHeader();
        this.setMonsterVisual('black_moon_knight', '委托板');

        var total = this.contracts.length * 4;
        var text = '短局结构：接委托 → 选伙伴 → 有限调查 → 战前准备 → 简单战斗 → 审判真相 → 收集结局。\n\n';
        text += this.buildLedgerLine() + '\n';
        text += '结局图鉴：' + this.getTotalContractEndings() + '/' + total + '\n\n';
        text += '每个委托会随机真凶、弱点、村民态度、天气和代价。';

        this.questLabel.string = '猎魔委托 Roguelite';
        this.storyTitle.string = '猎魔委托板';
        this.storyText.string = text;
        this.hintLabel.string = '每局约 3～5 分钟，选择会沉到声望里。';

        var choices = [];
        for (var i = 0; i < this.contracts.length; i++) {
            choices.push({
                text: this.contracts[i].title + ' ' + this.getContractEndingCount(this.contracts[i].id) + '/4',
                action: this.startContract.bind(this, this.contracts[i])
            });
        }
        choices.push({ text: '结局图鉴', action: this.showGallery.bind(this) });
        choices.push({ text: '返回入口', action: this.showHome.bind(this) });
        this.showChoices(choices);
    },

    startContract: function (contract) {
        var weaknessPool = ['silver', 'fire', 'blackBlood', 'ravenCharm'];
        this.run = {
            contract: contract,
            culprit: this.pick(contract.culprits),
            weakness: this.pick(weaknessPool),
            attitude: this.pick(contract.attitudes),
            weather: this.pick(contract.weather),
            cost: this.pick(contract.costs),
            partner: null,
            loadout: null,
            clues: [],
            selectedInvestigations: {},
            investigationsUsed: 0,
            maxInvestigations: 3,
            culpritKnown: false,
            weaknessKnown: false,
            attitudeKnown: false,
            weatherKnown: false,
            costKnown: false
        };
        this.playSfx(this.assets.sfx.confirm);
        this.showPartnerChoice();
    },

    showPartnerChoice: function () {
        this.mode = 'contract_partner';
        this.refreshHeader();
        this.setBackground(this.run.contract.background || 'first');
        this.setMonsterVisual(this.run.contract.kind, this.run.contract.monsterName);
        this.questLabel.string = '委托：选择同行者';
        this.storyTitle.string = '选择同行者';
        this.storyText.string = this.run.contract.title + '\n' + this.run.contract.briefing + '\n\n同行者会改变调查或战斗，也会改变村民看你的方式。\n' + this.buildReputationPressure();
        this.hintLabel.string = '不同伙伴会打开不同调查压力。';

        var choices = [];
        for (var i = 0; i < this.partners.length; i++) {
            choices.push({
                text: this.partners[i].name,
                action: this.selectPartner.bind(this, this.partners[i])
            });
        }
        choices.push({ text: '返回委托板', action: this.showContractBoard.bind(this) });
        this.showChoices(choices);
    },

    selectPartner: function (partner) {
        this.run.partner = partner;
        this.run.maxInvestigations = partner.kind === 'hound' ? 4 : 3;
        this.party = this.createContractParty(partner, null);

        if (partner.kind === 'smith') {
            this.ledger.gold = Math.max(0, this.ledger.gold - 5);
            this.saveLedger();
            this.run.clues.push('铁匠先收了五枚金币，只留下一句：别让刀口卷了。');
        } else if (partner.kind === 'nightCrow' && this.ledger.conscience >= 3) {
            this.run.weaknessKnown = true;
            this.run.clues.push('夜鸦女术士听见了怪物身上的裂口：它会回应' + this.getWeaknessName(this.run.weakness) + '。');
        }

        this.playSfx(this.assets.sfx.confirm);
        this.updatePartyView();
        this.showInvestigation('');
    },

    showInvestigation: function (newestClue) {
        this.mode = 'contract_investigation';
        this.refreshHeader();
        this.questLabel.string = '调查线索（' + this.run.investigationsUsed + '/' + this.run.maxInvestigations + '）';
        this.setBackground(this.run.contract.background || 'first');
        this.setMonsterVisual(this.run.contract.kind, this.run.contract.monsterName);
        this.storyTitle.string = '有限调查';
        var text = this.run.contract.title + '\n同行者：' + this.run.partner.name + '\n\n';
        if (newestClue) {
            text += '新线索：\n' + newestClue + '\n\n';
        }
        text += this.buildKnownFacts();
        this.storyText.string = text;
        this.hintLabel.string = this.run.investigationsUsed >= this.run.maxInvestigations
            ? '调查机会已用完，进入战前准备。'
            : '只能选有限线索，遗憾也是玩法的一部分。';

        if (this.run.investigationsUsed >= this.run.maxInvestigations) {
            this.showChoices([
                { text: '选择战前准备', action: this.showLoadout.bind(this) },
                { text: '返回委托板', action: this.showContractBoard.bind(this) }
            ]);
            return;
        }

        var names = ['调查尸体', '询问村长', '去教堂', '查看井口', '检查怪物脚印'];
        var choices = [];
        for (var i = 0; i < names.length; i++) {
            choices.push({
                text: this.run.selectedInvestigations[i] ? names[i] + '（已查）' : names[i],
                action: this.investigateContract.bind(this, i)
            });
        }
        choices.push({ text: '停止调查', action: this.showLoadout.bind(this) });
        this.showChoices(choices);
    },

    investigateContract: function (index) {
        if (this.run.selectedInvestigations[index] || this.run.investigationsUsed >= this.run.maxInvestigations) {
            return;
        }

        this.run.selectedInvestigations[index] = true;
        this.run.investigationsUsed++;
        var clue = this.resolveContractInvestigation(index);
        this.run.clues.push(clue);
        this.playSfx(this.assets.sfx.confirm);
        this.showInvestigation(clue);
    },

    resolveContractInvestigation: function (index) {
        switch (index) {
            case 0:
                this.run.culpritKnown = true;
                return '尸体的伤口不像求救，倒像临死前抓住了' + this.run.culprit + '留下的东西。';
            case 1:
                this.run.attitudeKnown = true;
                if (this.ledger.fear >= 3 && this.run.partner.kind !== 'nightCrow') {
                    return '村长把每句话都说得很慢。' + this.run.attitude + '不在词里，在他不肯抬头的那一下。';
                }
                if (this.run.partner.kind === 'nightCrow') {
                    this.run.culpritKnown = true;
                    return '夜鸦女术士听完证词后笑了一下：村民在' + this.run.attitude + '，真凶的名字绕回了' + this.run.culprit + '。';
                }
                return '村长说村民只想活到天亮，可门缝后的眼神更像' + this.run.attitude + '。';
            case 2:
                this.run.costKnown = true;
                if (this.run.weakness === 'fire' || this.run.weakness === 'ravenCharm') {
                    this.run.weaknessKnown = true;
                }
                return '教堂蜡泪结成' + this.getWeaknessName(this.run.weakness) + '的形状，祭台下压着一张写着“' + this.run.cost + '”的旧账。';
            case 3:
                this.run.weatherKnown = true;
                if (this.run.weakness === 'blackBlood') {
                    this.run.weaknessKnown = true;
                }
                return '井口的水没有倒影。今晚是' + this.run.weather + '，黑血药剂在瓶底轻轻发热。';
            default:
                this.run.weaknessKnown = true;
                return '怪物脚印到河边突然变浅，像被' + this.getWeaknessName(this.run.weakness) + '烫过。它不是只会吃人的东西。';
        }
    },

    showLoadout: function () {
        this.mode = 'contract_loadout';
        this.refreshHeader();
        this.questLabel.string = '战前准备';
        this.storyTitle.string = '战前准备';
        var text = '战斗会很短，但战前准备会兑现你调查到的东西。\n';
        text += this.run.weaknessKnown ? '你确信它怕' + this.getWeaknessName(this.run.weakness) + '。\n\n' : '你还没有确认弱点，只能带着猜测进雾里。\n\n';
        text += this.buildKnownFacts();
        this.storyText.string = text;
        this.hintLabel.string = '战斗不是操作考验，是调查结果的兑现。';

        var choices = [];
        for (var i = 0; i < this.loadouts.length; i++) {
            choices.push({
                text: this.loadouts[i].name,
                action: this.selectLoadout.bind(this, this.loadouts[i])
            });
        }
        choices.push({ text: '返回调查', action: this.showInvestigation.bind(this, '') });
        this.showChoices(choices);
    },

    selectLoadout: function (loadout) {
        this.run.loadout = loadout;
        this.party = this.createContractParty(this.run.partner, loadout);
        this.updatePartyView();
        this.playSfx(this.assets.sfx.confirm);

        var modifiers = this.buildContractBattleModifiers();
        var enemy = this.createEnemies(this.run.contract.kind, 1, this.run.contract.waveBonus, modifiers, this.run.contract.monsterName);
        var encounter = this.createEncounter(this.run.contract.monsterName, [enemy], modifiers.openingNote);
        this.startBattle(encounter, function () {
            this.showJudgement();
        }.bind(this), function () {
            this.showContractDefeat();
        }.bind(this));
    },

    showJudgement: function () {
        this.mode = 'contract_judgement';
        this.playMusic(this.assets.music.first);
        this.setBackground(this.run.contract.background || 'first');
        this.refreshHeader();
        this.questLabel.string = '审判真相';
        this.setMonsterVisual(this.run.contract.kind, this.run.contract.monsterName);
        this.storyTitle.string = '审判真相';
        this.storyText.string = this.run.contract.monsterName + '倒下了，但委托还没结束。\n\n' + this.buildKnownFacts() + '\n你可以让村子得到一个答案，也可以让答案继续待在黑水里。';
        this.hintLabel.string = '没有善恶按钮，只有你愿意背下来的后果。';
        this.showChoices([
            { text: '斩杀领赏', action: this.resolveJudgement.bind(this, 0) },
            { text: '揭穿真凶', action: this.resolveJudgement.bind(this, 1) },
            { text: '收钱沉默', action: this.resolveJudgement.bind(this, 2) },
            { text: '放走怪物', action: this.resolveJudgement.bind(this, 3) }
        ]);
    },

    resolveJudgement: function (choiceIndex) {
        var result = this.buildEndingResult(choiceIndex);
        this.ledger.gold = Math.max(0, this.ledger.gold + result.gold);
        this.ledger.conscience += result.conscience;
        this.ledger.fear = Math.max(0, this.ledger.fear + result.fear);
        this.ledger.fame += result.fame;
        this.saveLedger();
        this.unlockContractEnding(this.run.contract.id, result.id);
        this.playSfx(result.id === 'dark' ? this.assets.sfx.transition : this.assets.sfx.victory);
        this.showContractResult(result);
    },

    showContractResult: function (result) {
        this.mode = 'contract_result';
        this.refreshHeader();
        this.storyTitle.string = result.title;
        this.storyText.string = result.text + '\n\n变化：金币 ' + this.signed(result.gold) + '  良知 ' + this.signed(result.conscience) + '  恐惧 ' + this.signed(result.fear) + '  名声 ' + this.signed(result.fame) + '\n' + this.buildLedgerLine() + '\n《' + this.run.contract.title + '》已解锁：' + this.getContractEndingCount(this.run.contract.id) + '/4';
        this.hintLabel.string = '同一个委托，不同调查和审判会留下不同故事。';
        this.showChoices([
            { text: '返回委托板', action: this.showContractBoard.bind(this) },
            { text: '结局图鉴', action: this.showGallery.bind(this) }
        ]);
    },

    showContractDefeat: function () {
        this.mode = 'contract_defeat';
        this.playMusic(this.assets.music.first);
        this.refreshHeader();
        this.storyTitle.string = '委托失败';
        this.storyText.string = '你从夜色里退出来，伤口比答案更先抵达村口。\n' + this.run.contract.title + '还挂在委托板上，下面多了一道没人承认的抓痕。';
        this.hintLabel.string = '这局没有结局收集。';
        this.showChoices([
            { text: '返回委托板', action: this.showContractBoard.bind(this) },
            { text: '重开这个委托', action: this.startContract.bind(this, this.run.contract) }
        ]);
    },

    startBattle: function (encounter, onWin, onLose) {
        this.mode = 'battle';
        this.dialogue = null;
        this.setBackground('battle');
        this.playMusic(this.assets.music.battle);
        this.playSfx(this.assets.sfx.transition);

        this.battle = {
            encounter: encounter,
            enemies: encounter.enemies,
            party: this.clonePartyForBattle(this.party),
            turnUnits: [],
            activeUnit: null,
            turnNumber: 1,
            potionCount: 3,
            onWin: onWin,
            onLose: onLose,
            resolving: true
        };
        this.buildTurnUnits();
        this.refreshBattleView(encounter.openingNote || ('遭遇 ' + encounter.title + '！'));
        this.showChoices([]);
        this.scheduleOnce(function () {
            this.dispatchNextTurn();
        }.bind(this), 0.65);
    },

    dispatchNextTurn: function () {
        if (!this.battle) {
            return;
        }
        if (this.checkBattleEnded()) {
            return;
        }

        this.battle.resolving = true;
        var unit = this.takeNextTurnUnit();
        this.battle.activeUnit = unit;
        this.refreshBattleView('');

        if (!unit) {
            this.battle.resolving = false;
            return;
        }

        if (unit.side === 'party') {
            this.battle.resolving = false;
            this.showFriendlyTurn(unit);
        } else {
            this.showChoices([]);
            this.storyText.string = unit.name + '正在逼近。';
            this.scheduleOnce(function () {
                this.resolveEnemyTurn(unit);
            }.bind(this), 0.45);
        }
    },

    showFriendlyTurn: function (unit) {
        this.refreshBattleView(unit.name + '回合：选择行动。');
        this.showChoices([
            { text: '攻击', action: this.useFriendlyDefaultAttack.bind(this, unit) },
            { text: '技能', action: this.showSkillMenu.bind(this, unit) },
            { text: '药剂 x' + this.battle.potionCount, action: this.usePotion.bind(this, unit) },
            { text: '防御', action: this.useFriendlyDefend.bind(this, unit) }
        ]);
    },

    showSkillMenu: function (unit) {
        var skills = this.getFriendlySkills(unit);
        var choices = [];
        for (var i = 0; i < skills.length; i++) {
            choices.push({
                text: skills[i].name + (skills[i].cost > 0 ? ' MP' + skills[i].cost : ''),
                action: this.useSkill.bind(this, unit, skills[i])
            });
        }
        choices.push({ text: '返回', action: this.showFriendlyTurn.bind(this, unit) });
        this.refreshBattleView(unit.name + '：选择技能。');
        this.showChoices(choices);
    },

    useFriendlyDefaultAttack: function (unit) {
        this.useSkill(unit, this.getDefaultAttackSkill(unit));
    },

    useFriendlyDefend: function (unit) {
        this.useSkill(unit, this.getDefaultDefendSkill(unit));
    },

    usePotion: function (unit) {
        if (this.battle.potionCount <= 0) {
            this.refreshBattleView('药剂已经用完了。');
            return;
        }
        if (unit.hp >= unit.maxHp) {
            this.refreshBattleView('现在还不需要喝药。');
            return;
        }

        this.battle.potionCount--;
        this.useSkill(unit, this.createPotionSkill());
    },

    useSkill: function (caster, skill) {
        if (!this.battle || this.battle.resolving || !caster || !skill) {
            return;
        }
        if (caster.mp < skill.cost) {
            this.refreshBattleView('魔力不足，无法释放' + skill.name + '。');
            return;
        }

        this.battle.resolving = true;
        this.showChoices([]);
        caster.mp -= skill.cost;
        this.playSkillSfx(skill);

        var targets = this.selectTargets(skill, caster);
        var result = this.applySkill(caster, skill, targets);
        this.refreshBattleView(result.message);

        this.scheduleOnce(function () {
            if (this.checkBattleEnded()) {
                return;
            }

            this.consumeStatuses(caster);
            this.battle.turnNumber++;
            this.dispatchNextTurn();
        }.bind(this), result.defeated ? 0.95 : 0.62);
    },

    resolveEnemyTurn: function (unit) {
        if (!this.battle || !this.isAlive(unit)) {
            this.dispatchNextTurn();
            return;
        }

        var skill = this.chooseEnemySkill(unit);
        var targets = this.selectTargets(skill, unit);
        this.playSkillSfx(skill);
        var result = this.applySkill(unit, skill, targets);
        this.refreshBattleView(result.message);

        this.scheduleOnce(function () {
            if (this.checkBattleEnded()) {
                return;
            }
            this.consumeStatuses(unit);
            this.battle.turnNumber++;
            this.dispatchNextTurn();
        }.bind(this), 0.72);
    },

    applySkill: function (caster, skill, targets) {
        var message = skill.message || (caster.name + '使用' + skill.name + '。');
        var totalDamage = 0;
        var totalHeal = 0;
        var defeated = false;
        var targetNames = [];

        for (var t = 0; t < targets.length; t++) {
            var target = targets[t];
            if (!target || !this.isAlive(target)) {
                continue;
            }
            targetNames.push(target.name);

            for (var i = 0; i < skill.effects.length; i++) {
                var effect = skill.effects[i];
                if (effect.type === 'damage') {
                    for (var hit = 0; hit < (effect.hits || 1); hit++) {
                        var raw = Math.max(1, (effect.power || 0) + Math.round(this.getEffectiveAttack(caster) * (effect.attackScale || 0)) - Math.round(this.getEffectiveDefense(target) * (effect.defenseScale || 0)));
                        if (effect.damageType === 'fire' && target.weakness === 'fire') {
                            raw += 8;
                        }
                        if (effect.damageType === 'physical' && target.weakness === 'silver') {
                            raw += 5;
                        }
                        if (effect.damageType === 'arcane' && target.weakness === 'ravenCharm') {
                            raw += 6;
                        }
                        var damage = this.applyDamage(target, raw);
                        totalDamage += damage;
                    }
                    if (target.hp <= 0) {
                        defeated = true;
                    }
                } else if (effect.type === 'heal') {
                    var before = target.hp;
                    target.hp = Math.min(target.maxHp, target.hp + (effect.hp || 0));
                    target.mp = Math.min(target.maxMp, target.mp + (effect.mp || 0));
                    totalHeal += target.hp - before;
                } else if (effect.type === 'status') {
                    this.addStatus(target, this.clone(effect.status));
                }
            }
        }

        if (skill.id === 'bloodDrain' && caster.side === 'enemy' && totalDamage > 0) {
            var heal = Math.max(4, Math.ceil(totalDamage * 0.45));
            caster.hp = Math.min(caster.maxHp, caster.hp + heal);
            message = caster.name + '吸回 ' + heal + ' 点生命。';
        } else if (totalDamage > 0) {
            message += '\n对' + targetNames.join('、') + '造成 ' + totalDamage + ' 点伤害。';
        } else if (totalHeal > 0) {
            message += '\n恢复 ' + totalHeal + ' 点生命。';
        }

        if (defeated) {
            message += '\n敌人的动作慢了半拍，技能余波散尽后才倒下。';
        }

        return {
            message: message,
            defeated: defeated
        };
    },

    applyDamage: function (target, raw) {
        var damage = Math.max(0, raw);
        for (var i = 0; i < target.statuses.length; i++) {
            var status = target.statuses[i];
            if (status.incomingMultiplier !== undefined) {
                damage = Math.ceil(damage * status.incomingMultiplier);
            }
            if (status.shield && status.shield > 0) {
                var absorbed = Math.min(status.shield, damage);
                status.shield -= absorbed;
                damage -= absorbed;
            }
        }
        damage = Math.max(0, damage);
        target.hp = Math.max(0, target.hp - damage);
        return damage;
    },

    consumeStatuses: function (unit) {
        for (var i = unit.statuses.length - 1; i >= 0; i--) {
            var status = unit.statuses[i];
            if (status.dot && status.dot > 0 && unit.hp > 0) {
                unit.hp = Math.max(0, unit.hp - status.dot);
            }
            status.turns -= 1;
            if (status.turns <= 0 || (status.kind === 'shield' && status.shield <= 0)) {
                unit.statuses.splice(i, 1);
            }
        }
    },

    checkBattleEnded: function () {
        if (!this.battle) {
            return true;
        }

        if (this.getLivingParty().length === 0) {
            var lose = this.battle.onLose;
            this.battle = null;
            this.playSfx(this.assets.sfx.transition);
            if (lose) {
                lose();
            }
            return true;
        }

        if (this.getLivingEnemies().length === 0) {
            var win = this.battle.onWin;
            this.playSfx(this.assets.sfx.victory);
            this.refreshBattleView('战斗胜利。');
            this.showChoices([]);
            this.scheduleOnce(function () {
                this.battle = null;
                if (win) {
                    win();
                }
            }.bind(this), 0.8);
            return true;
        }

        return false;
    },

    refreshBattleView: function (message) {
        if (!this.battle) {
            return;
        }

        var firstEnemy = this.getLivingEnemies()[0] || this.battle.enemies[0];
        this.refreshHeader();
        this.questLabel.string = '战斗：第 ' + this.battle.turnNumber + ' 回合';
        if (firstEnemy) {
            this.setMonsterVisual(firstEnemy.kind, firstEnemy.name);
        }
        this.timelineLabel.string = this.buildTimelinePreview(5);
        this.enemyStatLabel.string = this.buildEnemyStatus();
        this.storyTitle.string = this.battle.encounter.title;
        this.storyText.string = message || this.battle.encounter.openingNote || '黑暗中的怪物逼近。';
        this.hintLabel.string = this.buildPartyStatus();
        this.updatePartyView();
    },

    buildTurnUnits: function () {
        this.battle.turnUnits = [];
        var i;
        for (i = 0; i < this.battle.party.length; i++) {
            if (this.battle.party[i].joined) {
                this.battle.turnUnits.push(this.battle.party[i]);
            }
        }
        for (i = 0; i < this.battle.enemies.length; i++) {
            this.battle.turnUnits.push(this.battle.enemies[i]);
        }
    },

    takeNextTurnUnit: function () {
        for (var step = 0; step < 80; step++) {
            var ready = null;
            for (var i = 0; i < this.battle.turnUnits.length; i++) {
                var unit = this.battle.turnUnits[i];
                if (!this.isAlive(unit) || unit.actionValue < 100) {
                    continue;
                }
                if (!ready || unit.actionValue > ready.actionValue) {
                    ready = unit;
                }
            }
            if (ready) {
                ready.actionValue -= 100;
                return ready;
            }
            for (var j = 0; j < this.battle.turnUnits.length; j++) {
                var next = this.battle.turnUnits[j];
                if (this.isAlive(next)) {
                    next.actionValue += Math.max(1, next.speed);
                }
            }
        }
        return null;
    },

    buildTimelinePreview: function (count) {
        if (!this.battle) {
            return '';
        }
        var sim = [];
        for (var i = 0; i < this.battle.turnUnits.length; i++) {
            var unit = this.battle.turnUnits[i];
            if (this.isAlive(unit)) {
                sim.push({ name: unit.name, speed: unit.speed, actionValue: unit.actionValue });
            }
        }
        var names = [];
        for (var n = 0; n < count; n++) {
            var readyIndex = -1;
            for (var step = 0; step < 80 && readyIndex < 0; step++) {
                for (var r = 0; r < sim.length; r++) {
                    if (sim[r].actionValue >= 100 && (readyIndex < 0 || sim[r].actionValue > sim[readyIndex].actionValue)) {
                        readyIndex = r;
                    }
                }
                if (readyIndex < 0) {
                    for (var s = 0; s < sim.length; s++) {
                        sim[s].actionValue += Math.max(1, sim[s].speed);
                    }
                }
            }
            if (readyIndex < 0) {
                break;
            }
            names.push(sim[readyIndex].name);
            sim[readyIndex].actionValue -= 100;
        }
        return names.length > 0 ? '顺序：' + names.join(' > ') : '';
    },

    selectTargets: function (skill, caster) {
        if (skill.target === 'self') {
            return [caster];
        }
        if (caster.side === 'enemy') {
            var livingParty = this.getLivingParty();
            if (livingParty.length <= 1) {
                return livingParty;
            }
            var index = Math.random() < 0.35 ? Math.min(1, livingParty.length - 1) : 0;
            return [livingParty[index]];
        }

        var livingEnemies = this.getLivingEnemies();
        if (skill.target === 'allEnemies') {
            return livingEnemies;
        }
        return livingEnemies.length > 0 ? [livingEnemies[0]] : [];
    },

    getLivingParty: function () {
        var result = [];
        if (!this.battle) {
            return result;
        }
        for (var i = 0; i < this.battle.party.length; i++) {
            if (this.isAlive(this.battle.party[i])) {
                result.push(this.battle.party[i]);
            }
        }
        return result;
    },

    getLivingEnemies: function () {
        var result = [];
        if (!this.battle) {
            return result;
        }
        for (var i = 0; i < this.battle.enemies.length; i++) {
            if (this.isAlive(this.battle.enemies[i])) {
                result.push(this.battle.enemies[i]);
            }
        }
        return result;
    },

    isAlive: function (unit) {
        return unit && unit.hp > 0;
    },

    getEffectiveAttack: function (unit) {
        var value = unit.attack || 0;
        if (unit.kind === 'triss' || unit.kind === 'yennefer' || unit.kind === 'pyromancer') {
            value = Math.max(value, unit.magic || 0);
        }
        for (var i = 0; i < unit.statuses.length; i++) {
            value += unit.statuses[i].attackBonus || 0;
        }
        return Math.max(0, value);
    },

    getEffectiveDefense: function (unit) {
        var value = unit.defense || 0;
        for (var i = 0; i < unit.statuses.length; i++) {
            value += unit.statuses[i].defenseBonus || 0;
        }
        return Math.max(0, value);
    },

    addStatus: function (unit, status) {
        if (!status) {
            return;
        }
        for (var i = unit.statuses.length - 1; i >= 0; i--) {
            if (unit.statuses[i].kind === status.kind) {
                unit.statuses.splice(i, 1);
            }
        }
        unit.statuses.push(status);
    },

    chooseEnemySkill: function (enemy) {
        var skills = this.getEnemySkills(enemy);
        if ((enemy.kind === 'black_moon_knight'
            || enemy.kind === 'black_nail_thrall'
            || enemy.kind === 'black_wax_gate_shade'
            || enemy.kind === 'black_wax_acolyte'
            || enemy.kind === 'black_nail_puppet'
            || enemy.kind === 'black_wax_saint') && this.battle.turnNumber % 3 === 0) {
            return this.createMoonbreaker();
        }
        var seed = Math.abs(this.battle.turnNumber * 37 + enemy.slot * 17 + enemy.hp);
        return skills[seed % skills.length];
    },

    getFriendlySkills: function (unit) {
        if (unit.kind === 'triss' || unit.kind === 'pyromancer') {
            return [
                this.createTrissFirebolt(),
                this.createTrissMeltingSigil(),
                this.createTrissFlameWard(),
                this.createTrissMeteorFlare()
            ];
        }
        if (unit.kind === 'yennefer') {
            return [
                this.createYenneferArcaneBolt(),
                this.createYenneferCursePulse(),
                this.createYenneferAegis(),
                this.createYenneferObsidianStorm()
            ];
        }
        return [
            this.createExecuteSlash(),
            this.createFlameSign(),
            this.createThunderSign(),
            this.createHunterFocus()
        ];
    },

    getDefaultAttackSkill: function (unit) {
        if (unit.kind === 'triss' || unit.kind === 'pyromancer') {
            return this.createTrissFirebolt();
        }
        if (unit.kind === 'yennefer') {
            return this.createYenneferArcaneBolt();
        }
        return this.createBasicAttack();
    },

    getDefaultDefendSkill: function (unit) {
        if (unit.kind === 'triss' || unit.kind === 'pyromancer') {
            return this.createTrissFlameWard();
        }
        if (unit.kind === 'yennefer') {
            return this.createYenneferAegis();
        }
        return this.createDefend();
    },

    getEnemySkills: function (enemy) {
        if (enemy.kind === 'blood_wraith' || enemy.kind === 'crowbone_stitcher') {
            return [this.createBloodDrain(), this.createPlagueHowl(), this.createCorruptedBite()];
        }
        if (enemy.kind === 'black_moon_knight'
            || enemy.kind === 'black_nail_thrall'
            || enemy.kind === 'black_wax_gate_shade'
            || enemy.kind === 'black_wax_acolyte'
            || enemy.kind === 'black_nail_puppet'
            || enemy.kind === 'black_wax_saint') {
            return [this.createMoonbreaker(), this.createPlagueHowl(), this.createBloodDrain()];
        }
        return [this.createCorruptedBite(), this.createPlagueHowl()];
    },

    createBasicAttack: function () {
        return this.skill('basicAttack', '银剑斩击', 0, 'firstEnemy', 'slash', '猎魔人挥出银剑。', [
            this.damage(0, 1, 1, 1, 'physical')
        ]);
    },

    createFlameSign: function () {
        return this.skill('flameSign', '火焰法印', 18, 'allEnemies', 'flame', '火焰法印横扫敌群！', [
            this.damage(26, 0, 0.5, 1, 'fire')
        ]);
    },

    createDefend: function () {
        return this.skill('defend', '防御', 0, 'self', 'defend', '猎魔人架起银剑，准备承受攻击。', [
            this.status('guard', '防御', 1, 0, 0, 0.45, 0, 0)
        ]);
    },

    createPotionSkill: function () {
        return this.skill('potion', '燕子药剂', 0, 'self', 'item', '喝下燕子药剂。', [
            { type: 'heal', hp: 32, mp: 0 }
        ]);
    },

    createExecuteSlash: function () {
        return this.skill('executeSlash', '连续斩杀', 12, 'firstEnemy', 'slash', '猎魔人连续压制目标！', [
            this.damage(2, 0.72, 0.8, 3, 'physical')
        ]);
    },

    createThunderSign: function () {
        return this.skill('thunderSign', '雷霆法印', 20, 'firstEnemy', 'cast', '雷霆法印劈向首个敌人！', [
            this.damage(30, 0, 0.25, 2, 'lightning')
        ]);
    },

    createHunterFocus: function () {
        return this.skill('hunterFocus', '猎魔专注', 10, 'self', 'cast', '猎魔人进入专注状态。', [
            this.status('attackUp', '专注', 3, 6, 0, 1, 0, 0)
        ]);
    },

    createTrissFirebolt: function () {
        return this.skill('trissFirebolt', '火焰术', 12, 'firstEnemy', 'flame', '特莉丝投出压缩火球，砸向首个敌人！', [
            this.damage(28, 0, 0.25, 1, 'fire')
        ]);
    },

    createTrissMeltingSigil: function () {
        return this.skill('trissMeltingSigil', '熔甲火印', 18, 'allEnemies', 'flame', '特莉丝点燃敌群护甲的缝隙，降低怪物防御！', [
            this.damage(16, 0, 0.2, 1, 'fire'),
            this.status('burning', '熔甲', 2, 0, -3, 1, 0, 0)
        ]);
    },

    createTrissFlameWard: function () {
        return this.skill('trissFlameWard', '灼热结界', 16, 'self', 'defend', '特莉丝在前排展开灼热结界。', [
            this.status('shield', '灼热结界', 3, 0, 0, 0.72, 22, 0)
        ]);
    },

    createTrissMeteorFlare: function () {
        return this.skill('trissMeteorFlare', '流星火雨', 28, 'allEnemies', 'flame', '特莉丝召下流星火雨，席卷敌群！', [
            this.damage(34, 0, 0.18, 1, 'fire')
        ]);
    },

    createYenneferArcaneBolt: function () {
        return this.skill('yenneferArcaneBolt', '紫晶箭', 12, 'firstEnemy', 'cast', '叶奈法凝出紫晶箭，贯穿首个敌人！', [
            this.damage(24, 0, 0.25, 1, 'arcane')
        ]);
    },

    createYenneferCursePulse: function () {
        return this.skill('yenneferCursePulse', '诅咒脉冲', 18, 'allEnemies', 'cast', '叶奈法释放诅咒脉冲，削弱敌群防御！', [
            this.damage(14, 0, 0.22, 1, 'arcane'),
            this.status('fear', '诅咒', 2, 0, -3, 1, 0, 0)
        ]);
    },

    createYenneferAegis: function () {
        return this.skill('yenneferAegis', '紫晶护盾', 16, 'self', 'defend', '叶奈法为前排展开紫晶护盾。', [
            this.status('shield', '紫晶护盾', 3, 0, 0, 0.7, 24, 0)
        ]);
    },

    createYenneferObsidianStorm: function () {
        return this.skill('yenneferObsidianStorm', '黑曜风暴', 28, 'allEnemies', 'cast', '叶奈法召来黑曜碎光，席卷敌群！', [
            this.damage(30, 0, 0.2, 1, 'arcane')
        ]);
    },

    createCorruptedBite: function () {
        return this.skill('corruptedBite', '腐毒撕咬', 0, 'firstEnemy', 'slash', '怪物扑咬猎魔人，污血渗入伤口！', [
            this.damage(3, 1, 0.45, 1, 'physical'),
            this.status('corruption', '腐毒', 2, 0, 0, 1.08, 0, 4)
        ]);
    },

    createPlagueHowl: function () {
        return this.skill('plagueHowl', '瘟疫嚎叫', 0, 'firstEnemy', 'cast', '怪物发出刺耳嚎叫，猎魔人的攻势被压住了！', [
            this.damage(0, 0.45, 0.25, 1, 'physical'),
            this.status('fear', '恐惧', 2, -4, -2, 1, 0, 0)
        ]);
    },

    createBloodDrain: function () {
        return this.skill('bloodDrain', '吸血咒吻', 0, 'firstEnemy', 'cast', '血影缠住猎魔人，怪物从伤口里夺回生命！', [
            this.damage(7, 0.85, 0.35, 1, 'arcane')
        ]);
    },

    createMoonbreaker: function () {
        return this.skill('moonbreaker', '黑月断斩', 0, 'firstEnemy', 'slash', '月夜骑士拖出黑月般的剑痕！', [
            this.damage(14, 1.15, 0.55, 2, 'pure')
        ]);
    },

    skill: function (id, name, cost, target, animation, message, effects) {
        return {
            id: id,
            name: name,
            cost: cost,
            target: target,
            animation: animation,
            message: message,
            effects: effects || []
        };
    },

    damage: function (power, attackScale, defenseScale, hits, damageType) {
        return {
            type: 'damage',
            power: power,
            attackScale: attackScale,
            defenseScale: defenseScale,
            hits: hits,
            damageType: damageType || 'physical'
        };
    },

    status: function (kind, name, turns, attackBonus, defenseBonus, incomingMultiplier, shield, dot) {
        return {
            type: 'status',
            status: {
                kind: kind,
                name: name,
                turns: turns,
                attackBonus: attackBonus,
                defenseBonus: defenseBonus,
                incomingMultiplier: incomingMultiplier,
                shield: shield,
                dot: dot
            }
        };
    },

    buildMainBossModifiers: function (nightNumber, flags, choiceIndex, truthCorrect) {
        var m = {
            healthPenalty: 0,
            attackPenalty: truthCorrect ? 3 : 1,
            defensePenalty: 0,
            shieldAdjustment: 0,
            weakness: 'silver',
            weaknessKnown: false,
            openingNote: ''
        };

        if (nightNumber === 1) {
            m.healthPenalty = flags.blackBlood ? 10 : 0;
            m.defensePenalty = flags.falseTestimony ? 2 : 0;
            m.shieldAdjustment = flags.clawMarks ? -1 : 0;
            m.weaknessKnown = !!flags.blackBlood;
            if (choiceIndex === 0) {
                m.healthPenalty += 3;
                m.attackPenalty = 0;
                m.defensePenalty += 1;
                m.shieldAdjustment += 1;
                m.openingNote = '选择后果：井口被封，村民安静下来；井底哭魂在黑暗里撞得更急。';
            } else if (choiceIndex === 1) {
                m.healthPenalty += 10;
                m.attackPenalty = 2;
                m.defensePenalty += flags.falseTestimony ? 3 : 1;
                m.openingNote = '选择后果：献祭者的名字被说出口，伪装变薄；村里有人开始恨你。';
            } else {
                m.healthPenalty += 16;
                m.attackPenalty = 3;
                m.shieldAdjustment -= 2;
                m.openingNote = '选择后果：你先救活人，哭声仍在；井底哭魂露出最深的一道裂缝。';
            }
            return m;
        }

        if (nightNumber === 2) {
            m.healthPenalty = flags.ironCorpse ? 8 : 0;
            m.defensePenalty = flags.blackenedHammer ? 2 : 0;
            m.shieldAdjustment = flags.chapelWax ? -1 : 0;
            m.weakness = flags.blackenedHammer ? 'fire' : 'silver';
            m.weaknessKnown = !!flags.blackenedHammer;
            if (choiceIndex === 0) {
                m.healthPenalty += 2;
                m.attackPenalty = 0;
                m.shieldAdjustment += 1;
                m.openingNote = '选择后果：铁匠成了村口的答案；黑钉傀儡像收到命令一样站稳。';
            } else if (choiceIndex === 1) {
                m.healthPenalty += 6;
                m.attackPenalty = 1;
                m.defensePenalty += 2;
                m.openingNote = '选择后果：教堂保住了门面；黑蜡也保住了傀儡的骨架。';
            } else {
                m.healthPenalty += 14;
                m.attackPenalty = 3;
                m.shieldAdjustment -= 2;
                m.openingNote = '选择后果：银钉被拔出，尸体倒下；黑钉傀儡失去一半操控节奏。';
            }
            return m;
        }

        m.healthPenalty = flags.cryptGate ? 8 : 0;
        m.defensePenalty = flags.blackWaxAltar ? 2 : 0;
        m.shieldAdjustment = flags.sealedReliquary ? -1 : 0;
        m.weakness = 'fire';
        m.weaknessKnown = !!flags.blackWaxAltar;
        m.attackPenalty = truthCorrect ? 4 : 1;
        if (choiceIndex === 0) {
            m.healthPenalty += 12;
            m.attackPenalty = 2;
            m.shieldAdjustment -= 1;
            m.openingNote = '选择后果：特莉丝烧开黑蜡，圣徒的外壳变薄；那些被蜡封住的名字也一起安静了。';
        } else if (choiceIndex === 1) {
            m.healthPenalty += 4;
            m.attackPenalty = 0;
            m.defensePenalty += 2;
            m.shieldAdjustment += 2;
            m.openingNote = '选择后果：地下门被封住，村里暂时睡去；黑蜡圣徒像守门人一样站得更稳。';
        } else {
            m.healthPenalty += 18;
            m.attackPenalty = 4;
            m.shieldAdjustment -= 2;
            m.openingNote = '选择后果：银钉交到孩子手里，圣匣自己裂开；黑蜡圣徒失去了最顺手的命令。';
        }
        return m;
    },

    buildContractBattleModifiers: function () {
        var m = {
            healthPenalty: this.run.investigationsUsed * 2,
            attackPenalty: this.run.culpritKnown ? 1 : 0,
            defensePenalty: this.run.weaknessKnown ? 1 : 0,
            shieldAdjustment: 0,
            weakness: this.run.weakness,
            weaknessKnown: this.run.weaknessKnown,
            openingNote: ''
        };

        var matched = this.run.loadout.isWeaknessPrep && this.run.loadout.weakness === this.run.weakness;
        if (matched) {
            m.healthPenalty += 24;
            m.attackPenalty += 4;
            m.defensePenalty += 2;
            m.shieldAdjustment -= 2;
        } else if (this.run.loadout.isWeaknessPrep) {
            m.healthPenalty += this.run.weaknessKnown ? 4 : 0;
            m.shieldAdjustment += 1;
        } else if (this.run.loadout.isDefense) {
            m.attackPenalty += 3;
            m.shieldAdjustment -= 1;
        }

        if (this.run.partner.kind === 'pyromancer' && this.run.loadout.weakness === 'fire') {
            m.healthPenalty += 8;
            if (this.run.attitudeKnown && this.run.attitude === '求救') {
                m.attackPenalty += 1;
            }
        } else if (this.run.partner.kind === 'smith' && this.run.loadout.weakness === 'silver') {
            m.healthPenalty += 8;
        } else if (this.run.partner.kind === 'nightCrow' && this.run.culpritKnown) {
            m.defensePenalty += 1;
        } else if (!this.run.loadout.isWeaknessPrep && !this.run.loadout.isDefense && this.run.culpritKnown) {
            m.attackPenalty += 2;
            m.defensePenalty += 1;
        }

        if (this.ledger.gold >= 60 && this.run.loadout.isWeaknessPrep) {
            m.healthPenalty += 6;
        }
        if (this.run.weather === '暴雨' && this.run.loadout.weakness === 'fire') {
            m.healthPenalty = Math.max(0, m.healthPenalty - 6);
        } else if (this.run.weather === '血月') {
            m.attackPenalty = Math.max(0, m.attackPenalty - 2);
        }

        m.openingNote = (this.run.weatherKnown ? '天气兑现：' + this.run.weather + '压在屋顶上。 ' : '你没有弄清今晚的天色，只能听见远处的水声。 ');
        m.openingNote += matched
            ? '准备兑现：' + this.run.loadout.name + '正好咬住它怕的' + this.getWeaknessName(this.run.weakness) + '。'
            : '准备偏了：你带着' + this.run.loadout.name + '进场，怪物没有退。';
        if (this.run.culpritKnown) {
            m.openingNote += ' 你记得线索指向' + this.run.culprit + '，剑没有先急着落下。';
        }
        return m;
    },

    createEncounter: function (title, enemyGroups, openingNote) {
        var enemies = [];
        for (var i = 0; i < enemyGroups.length; i++) {
            var group = enemyGroups[i];
            for (var j = 0; j < group.length; j++) {
                group[j].slot = enemies.length;
                enemies.push(group[j]);
            }
        }
        return {
            title: title,
            enemies: enemies,
            openingNote: openingNote
        };
    },

    createEnemies: function (kind, count, waveBonus, modifiers, nameOverride) {
        var result = [];
        var base = this.getEnemyBase(kind, waveBonus || 0);
        var safeCount = Math.max(1, count || 1);
        for (var i = 0; i < safeCount; i++) {
            var name = nameOverride || base.name;
            if (safeCount > 1) {
                name += ' ' + (i + 1);
            }
            var hp = Math.max(1, base.hp - (modifiers ? modifiers.healthPenalty : 0));
            result.push({
                side: 'enemy',
                kind: kind,
                name: name,
                maxHp: hp,
                hp: hp,
                maxMp: 0,
                mp: 0,
                attack: Math.max(1, base.attack - (modifiers ? modifiers.attackPenalty : 0)),
                defense: Math.max(0, base.defense - (modifiers ? modifiers.defensePenalty : 0) + (modifiers ? modifiers.shieldAdjustment : 0)),
                magic: 0,
                speed: base.speed,
                actionValue: 0,
                statuses: [],
                weakness: modifiers && modifiers.weakness ? modifiers.weakness : base.weakness,
                weaknessKnown: modifiers ? modifiers.weaknessKnown : false,
                rewardGold: base.gold,
                rewardExp: base.exp
            });
        }
        return result;
    },

    getEnemyBase: function (kind, bonus) {
        bonus = Math.max(0, bonus || 0);
        switch (kind) {
            case 'blood_wraith':
                return { name: '血怨灵', hp: 34 + bonus * 5, attack: 10 + bonus, defense: 3, speed: 98, gold: 13 + bonus * 3, exp: 4, weakness: 'ravenCharm' };
            case 'black_nail_thrall':
                return { name: '黑钉残尸', hp: 40 + bonus * 5, attack: 11 + bonus, defense: 4, speed: 86, gold: 15 + bonus * 3, exp: 5, weakness: 'fire' };
            case 'black_wax_gate_shade':
                return { name: '黑蜡守门影', hp: 42 + bonus * 6, attack: 11 + bonus, defense: 4, speed: 90, gold: 16 + bonus * 4, exp: 5, weakness: 'fire' };
            case 'black_wax_acolyte':
                return { name: '执钉黑蜡侍', hp: 48 + bonus * 6, attack: 12 + bonus, defense: 5, speed: 82, gold: 18 + bonus * 4, exp: 6, weakness: 'fire' };
            case 'crowbone_stitcher':
                return { name: '鸦骨缝尸', hp: 44 + bonus * 5, attack: 11 + bonus, defense: 4, speed: 92, gold: 16 + bonus * 3, exp: 5, weakness: 'ravenCharm' };
            case 'black_moon_knight':
                return { name: '月夜骑士', hp: 72 + bonus * 8, attack: 13 + bonus, defense: 5, speed: 84, gold: 38 + bonus * 8, exp: 9, weakness: 'silver' };
            case 'black_nail_puppet':
                return { name: '黑钉傀儡', hp: 88 + bonus * 10, attack: 12 + bonus, defense: 6, speed: 78, gold: 45 + bonus * 8, exp: 11, weakness: 'fire' };
            case 'black_wax_saint':
                return { name: '黑蜡圣徒', hp: 104 + bonus * 10, attack: 14 + bonus, defense: 6, speed: 76, gold: 52 + bonus * 8, exp: 13, weakness: 'fire' };
            default:
                return { name: '腐化狼', hp: 26 + bonus * 4, attack: 8 + bonus, defense: 2, speed: 108, gold: 8 + bonus * 2, exp: 3, weakness: 'silver' };
        }
    },

    createStoryParty: function (withTriss) {
        var party = [this.createHunter()];
        if (withTriss) {
            party.push(this.createTriss());
        }
        return party;
    },

    createContractParty: function (partner, loadout) {
        var party = [this.createHunter()];
        if (partner && partner.kind === 'pyromancer') {
            var pyro = this.createTriss();
            pyro.name = '赤焰术师';
            pyro.kind = 'pyromancer';
            party.push(pyro);
        } else if (partner && partner.kind === 'nightCrow') {
            party.push(this.createYennefer('夜鸦女术士'));
        }
        if (loadout && loadout.name === '女巫伙伴' && party.length === 1) {
            party.push(this.createTriss());
        }
        return party;
    },

    createHunter: function () {
        return { side: 'party', kind: 'hunter', portrait: 'hunter', name: '猎魔人', maxHp: 120, hp: 120, maxMp: 100, mp: 100, attack: 18, defense: 7, magic: 6, speed: 116, actionValue: 0, statuses: [], joined: true };
    },

    createTriss: function () {
        return { side: 'party', kind: 'triss', portrait: 'triss', name: '特莉丝', maxHp: 102, hp: 102, maxMp: 126, mp: 126, attack: 11, defense: 6, magic: 22, speed: 112, actionValue: 0, statuses: [], joined: true };
    },

    createYennefer: function (name) {
        return { side: 'party', kind: 'yennefer', portrait: 'yennefer', name: name || '叶奈法', maxHp: 88, hp: 88, maxMp: 142, mp: 142, attack: 8, defense: 5, magic: 24, speed: 108, actionValue: 0, statuses: [], joined: true };
    },

    clonePartyForBattle: function (party) {
        var result = [];
        for (var i = 0; i < party.length; i++) {
            var copy = this.clone(party[i]);
            copy.actionValue = 0;
            copy.statuses = [];
            result.push(copy);
        }
        return result;
    },

    recoverParty: function (full) {
        for (var i = 0; i < this.party.length; i++) {
            var unit = this.party[i];
            if (full) {
                unit.hp = unit.maxHp;
                unit.mp = unit.maxMp;
            } else {
                unit.hp = Math.min(unit.maxHp, unit.hp + 22);
                unit.mp = Math.min(unit.maxMp, unit.mp + 16);
            }
        }
        this.updatePartyView();
    },

    buildStoryData: function () {
        return {
            nights: {
                1: {
                    title: '第一晚',
                    subtitle: '老井哭声案',
                    background: 'first',
                    music: 'first',
                    nextNight: 2,
                    briefing: [
                        { speaker: '委托', text: '第一晚：老井哭声案。村口尸体还没凉，井里却传来女人哭声。' },
                        { speaker: '猎魔人', text: '先查线索。黑血、爪痕、假供词……它们会告诉我该用什么杀死它。' }
                    ],
                    steps: [
                        {
                            type: 'node',
                            label: '老井',
                            objective: '调查老井旁的黑血，确认怪物弱点',
                            prompt: '井沿凝着黑色血迹，银粉碰上去时发出轻微嘶鸣。',
                            flag: 'blackBlood',
                            clue: '老井黑血：银剑能伤它。',
                            previewKind: 'blood_wraith',
                            lines: [
                                { speaker: '老井', text: '井沿凝着黑色血迹，银粉碰上去时发出轻微嘶鸣。' },
                                { speaker: '猎魔人', text: '不是普通亡魂。银剑能伤它。' }
                            ],
                            ambush: { title: '井边腐兽', name: '井边腐兽', kind: 'corrupted_wolf', count: 1, openingNote: '井水猛地缩回黑暗，腐兽从井边扑出。' }
                        },
                        {
                            type: 'node',
                            label: '老磨坊',
                            objective: '调查老磨坊的爪痕，削弱护盾',
                            prompt: '木门上有反复抓挠的沟痕，却没有从外面破门的痕迹。',
                            flag: 'clawMarks',
                            clue: '磨坊爪痕：它曾被困住，护盾不会完整。',
                            previewKind: 'corrupted_wolf',
                            lines: [
                                { speaker: '老磨坊', text: '木门上有反复抓挠的沟痕，却没有从外面破门的痕迹。' },
                                { speaker: '猎魔人', text: '它被困过。它的护盾不会完整。' }
                            ],
                            ambush: { title: '磨坊哭影', name: '磨坊哭影', kind: 'blood_wraith', count: 1, openingNote: '磨坊梁柱后响起女人哭声，哭影从木屑里站起。' }
                        },
                        {
                            type: 'node',
                            label: '寡妇家',
                            objective: '询问寡妇家的假供词，判断真相',
                            prompt: '寡妇的门开着，桌上放着两只杯子，灰却只有一圈。',
                            flag: 'falseTestimony',
                            clue: '寡妇假供词：哭声不是引诱，是警告。',
                            previewKind: 'blood_wraith',
                            lines: [
                                { speaker: '寡妇', text: '我听见哭声从井底来……不，从磨坊来。别问了，猎魔人。' },
                                { speaker: '猎魔人', text: '她在撒谎。哭声不是引诱，是警告。有人还想继续献祭。' }
                            ]
                        },
                        {
                            type: 'choice',
                            objective: '决定如何处理井口哭声',
                            summary: '井口哭声停下前，你决定：\nA 封井止哭   B 公开献祭者   C 先救活人',
                            labels: ['A 封井', 'B 公开', 'C 救人'],
                            correctIndex: 2,
                            previewKind: 'black_moon_knight',
                            choiceLines: [
                                [
                                    { speaker: '猎魔人', text: '井盖落下时，哭声像被塞进石头里。村口的人第一次敢靠近火堆。' },
                                    { speaker: '寡妇', text: '她还在下面。你们只是听不见了。' }
                                ],
                                [
                                    { speaker: '猎魔人', text: '献祭者的名字被说出口后，村民都低下头，像突然认得自己的鞋。' },
                                    { speaker: '委托', text: '井底的哭声变轻了。寡妇家的窗却再也没有亮。' }
                                ],
                                [
                                    { speaker: '猎魔人', text: '你把还活着的人从井边拖开。哭声没有停，却开始给你让路。' },
                                    { speaker: '委托', text: '井水翻起白雾，像有人在下面松了一口气。' }
                                ]
                            ],
                            boss: { title: '井底哭魂', name: '井底哭魂', kind: 'black_moon_knight', waveBonus: 0 }
                        }
                    ],
                    settlements: [
                        [
                            { speaker: '委托结算', text: '村民把银币放在桌上。那一晚没人再听见哭声，连寡妇也没有。' },
                            { speaker: '第二晚钩子', text: '清晨，封井的铁链断了一节。村口又多了一具穿着铁匠围裙的尸体。' }
                        ],
                        [
                            { speaker: '委托结算', text: '银币被推到你面前时，几个人离席了。寡妇家的门被钉上一块木板。' },
                            { speaker: '第二晚钩子', text: '天亮前，铁匠铺的炉火自己灭了。村口多了一具穿着铁匠围裙的尸体。' }
                        ],
                        [
                            { speaker: '委托结算', text: '孩子醒来后没有说话，只把一枚湿透的银币塞进你手心。' },
                            { speaker: '第二晚钩子', text: '清晨，井边多了一排小脚印。脚印尽头，是一具穿着铁匠围裙的尸体。' }
                        ]
                    ]
                },
                2: {
                    title: '第二晚',
                    subtitle: '铁匠黑血案',
                    background: 'second',
                    music: 'second',
                    nextNight: 3,
                    briefing: [
                        { speaker: '委托', text: '第二晚：铁匠黑血案。天亮前，村口多了一具穿着铁匠围裙的尸体。' },
                        { speaker: '猎魔人', text: '铁匠未必死了。先看尸体、锤子和教堂。死人不会自己换衣服，除非有人想让它看起来像铁匠。' }
                    ],
                    steps: [
                        {
                            type: 'node',
                            label: '围裙尸体',
                            objective: '检查村口铁匠围裙尸体',
                            prompt: '尸体穿着铁匠的围裙，手指却细得像从未握过锤。',
                            flag: 'ironCorpse',
                            clue: '围裙尸体：这不是铁匠，有人把死者装成他。',
                            previewKind: 'black_nail_thrall',
                            lines: [
                                { speaker: '围裙尸体', text: '尸体穿着铁匠的围裙，手指却细得像从未握过锤。脖颈后钉着一枚发黑银钉。' },
                                { speaker: '猎魔人', text: '这不是铁匠。有人把死者装成他，想把我的剑引向错的人。' }
                            ]
                        },
                        {
                            type: 'node',
                            label: '黑血铁锤',
                            objective: '调查铁匠铺染黑的锤子',
                            prompt: '铁锤缝隙里有黑血，火星碰到血迹时炸出一圈蓝白色寒光。',
                            flag: 'blackenedHammer',
                            clue: '黑血铁锤：黑血怕火，也怕银。',
                            previewKind: 'black_nail_puppet',
                            lines: [
                                { speaker: '黑血铁锤', text: '铁锤缝隙里有黑血，火星碰到血迹时炸出一圈蓝白色寒光。' },
                                { speaker: '猎魔人', text: '黑血怕火，也怕银。今晚的东西不是肉身，是被钉住的怨念。' }
                            ],
                            ambush: { title: '黑钉残尸群', name: '黑钉残尸', kind: 'black_nail_thrall', count: 2, openingNote: '黑血沿地缝爬开，两个残尸被银钉拽着站起来。' }
                        },
                        {
                            type: 'node',
                            label: '教堂黑蜡',
                            objective: '调查教堂门口的黑蜡',
                            prompt: '黑蜡混着铁屑，凝成倒置的祷文。',
                            flag: 'chapelWax',
                            clue: '教堂黑蜡：神父在用银钉操控尸体。',
                            previewKind: 'black_wax_acolyte',
                            lines: [
                                { speaker: '教堂黑蜡', text: '教堂门口的蜡泪混着铁屑，凝成倒置的祷文。祷文最后一行写着：死者替活人赎罪。' },
                                { speaker: '猎魔人', text: '神父在用银钉操控尸体。找到蜡源，傀儡的护盾会裂开。' }
                            ],
                            ambush: { title: '执钉黑蜡侍', name: '执钉黑蜡侍', kind: 'black_wax_acolyte', count: 1, openingNote: '教堂门后的黑蜡滴到地上，拧出一个执钉黑影。' }
                        },
                        {
                            type: 'choice',
                            objective: '决定如何处理第二具行走尸体',
                            summary: '第二具尸体站起来后，你把真相交给谁？\nA 交出铁匠   B 保住教堂   C 拔掉银钉',
                            labels: ['A 铁匠', 'B 教堂', 'C 银钉'],
                            correctIndex: 2,
                            previewKind: 'black_nail_puppet',
                            choiceLines: [
                                [
                                    { speaker: '铁匠', text: '他们把铁匠铺的火灭了。没人问炉灰里为什么有教堂的蜡。' },
                                    { speaker: '猎魔人', text: '尸体听见这个结果，反而站得更直。' }
                                ],
                                [
                                    { speaker: '教堂', text: '钟声响了三下，村民跪下时很安静。黑蜡沿台阶往上爬。' },
                                    { speaker: '猎魔人', text: '有些门被保住了。也有些东西被关在门后。' }
                                ],
                                [
                                    { speaker: '猎魔人', text: '第一枚银钉拔出来时，尸体倒回泥里，像终于记起自己已经死了。' },
                                    { speaker: '委托', text: '铁匠铺的火重新亮起。教堂没有开门。' }
                                ]
                            ],
                            boss: { title: '黑钉傀儡', name: '黑钉傀儡', kind: 'black_nail_puppet', waveBonus: 1 }
                        }
                    ],
                    settlements: [
                        [
                            { speaker: '委托结算', text: '铁匠铺门口堆满石头。村民说这样睡得踏实些。' },
                            { speaker: '第三晚钩子', text: '夜里，石头缝里渗出黑蜡。教堂地下传来敲铁的声音。' }
                        ],
                        [
                            { speaker: '委托结算', text: '神父房间的灯亮了一整夜。村民路过时都放轻脚步。' },
                            { speaker: '第三晚钩子', text: '天快亮时，钟楼落下一根银钉，钉尖指向地下。' }
                        ],
                        [
                            { speaker: '委托结算', text: '傀儡倒下后，铁匠坐在炉前，把每一枚银钉都敲弯。' },
                            { speaker: '第三晚钩子', text: '夜里，教堂门自己开了。门内没有神父，只有一排刚点燃的蜡。' }
                        ]
                    ]
                },
                3: {
                    title: '第三晚',
                    subtitle: '黑蜡地下教堂',
                    background: 'third',
                    music: 'third',
                    nextNight: 0,
                    briefing: [
                        { speaker: '特莉丝', text: '黑蜡不是为了让死人站起来。它在给活人留一条下去的路。' },
                        { speaker: '猎魔人', text: '那就一起下去。你看火，我看刀。' }
                    ],
                    steps: [
                        {
                            type: 'node',
                            label: '地下门',
                            objective: '调查通往地下教堂的黑蜡门',
                            prompt: '门缝里没有风，只有热。黑蜡沿着石阶往下流。',
                            flag: 'cryptGate',
                            clue: '地下门：特莉丝能烧开门上的蜡，但火会留下代价。',
                            previewKind: 'black_wax_gate_shade',
                            lines: [
                                { speaker: '地下门', text: '门缝里没有风，只有热。黑蜡沿着石阶往下流，像有人在下面点着一整排蜡烛。' },
                                { speaker: '特莉丝', text: '我能烧开门上的蜡，但它们会记住我的火。' }
                            ],
                            ambush: { title: '黑蜡守门影', name: '黑蜡守门影', kind: 'black_wax_gate_shade', count: 2, openingNote: '黑蜡门吐出守门影，特莉丝的火光照出它们没有脸。' }
                        },
                        {
                            type: 'node',
                            label: '黑蜡祭坛',
                            objective: '调查地下教堂中央的黑蜡祭坛',
                            prompt: '祭坛上没有神像，只有一圈烧短的蜡。',
                            flag: 'blackWaxAltar',
                            clue: '黑蜡祭坛：这里不是献祭，是把人留在选择里。',
                            previewKind: 'black_wax_acolyte',
                            lines: [
                                { speaker: '黑蜡祭坛', text: '祭坛上没有神像，只有一圈烧短的蜡。每根蜡烛里都封着一小段头发。' },
                                { speaker: '猎魔人', text: '不是献祭。更像是把人留在这里，等有人替他们选择。' }
                            ],
                            ambush: { title: '蜡下腐兽', name: '蜡下腐兽', kind: 'corrupted_wolf', count: 2, openingNote: '蜡层裂开，腐兽拖着未干的黑火冲出祭坛。' }
                        },
                        {
                            type: 'node',
                            label: '封钉圣匣',
                            objective: '检查封着银钉的圣匣',
                            prompt: '圣匣里躺着三枚银钉，每枚钉帽上都刻着一个孩子的名字。',
                            flag: 'sealedReliquary',
                            clue: '封钉圣匣：银钉锁住的不是怪物，是孩子必须面对的恐惧。',
                            previewKind: 'black_nail_puppet',
                            lines: [
                                { speaker: '封钉圣匣', text: '圣匣里躺着三枚银钉。每枚钉帽上都刻着一个孩子的名字，最小的那个还没干。' },
                                { speaker: '特莉丝', text: '烧掉它，门会开。留下它，村里今晚能睡。交出去……他们就得自己醒着。' }
                            ]
                        },
                        {
                            type: 'choice',
                            objective: '决定如何处理圣匣里的银钉',
                            summary: '圣匣里的银钉还温着，你决定：\nA 让特莉丝烧尽黑蜡   B 封住地下门   C 把银钉交给孩子',
                            labels: ['A 烧蜡', 'B 封门', 'C 交钉'],
                            correctIndex: 2,
                            previewKind: 'black_wax_saint',
                            choiceLines: [
                                [
                                    { speaker: '特莉丝', text: '火从圣匣里翻出来时，黑蜡像雪一样塌下去。那些名字也跟着变轻了。' },
                                    { speaker: '猎魔人', text: '门开了。里面的东西没有哭，只是在等我们。' }
                                ],
                                [
                                    { speaker: '猎魔人', text: '你把门重新封上。村里钟声停了，地下却多了一次敲门声。' },
                                    { speaker: '特莉丝', text: '有些安静，不是结束。只是还没有轮到他们说话。' }
                                ],
                                [
                                    { speaker: '特莉丝', text: '孩子接过银钉时没有哭。他只是问：如果我怕，能不能晚一点敲下去。' },
                                    { speaker: '猎魔人', text: '你没有替他回答。圣匣自己裂开了一道缝。' }
                                ]
                            ],
                            boss: { title: '黑蜡圣徒', name: '黑蜡圣徒', kind: 'black_wax_saint', waveBonus: 2 }
                        }
                    ],
                    settlements: [
                        [
                            { speaker: '委托结算', text: '地下教堂亮了一整夜。天亮后，村民发现每扇窗台上都有一撮黑灰。' },
                            { speaker: '特莉丝', text: '我不知道我们救下了谁，也不知道谁被我们烧没了。' }
                        ],
                        [
                            { speaker: '委托结算', text: '村里终于睡了一个安稳觉。第三天清晨，教堂门口多了一只从里面伸出的手印。' },
                            { speaker: '猎魔人', text: '有些门能挡住怪物，也能挡住求救。' }
                        ],
                        [
                            { speaker: '委托结算', text: '孩子把银钉埋在井边，没有告诉任何人。那晚之后，井水第一次映出了星星。' },
                            { speaker: '特莉丝', text: '他还会害怕。可这次，害怕是他自己的。' }
                        ]
                    ]
                }
            }
        };
    },

    buildContracts: function () {
        var common = {
            culprits: ['村长', '女巫', '怪物', '被害人自己'],
            attitudes: ['隐瞒', '求救', '欺骗', '出卖你'],
            weather: ['大雾', '暴雨', '血月', '无月夜'],
            costs: ['救孩子', '保村子', '拿赏金', '放走怪物']
        };
        return [
            this.extend(common, { id: 'black_marsh_cry', title: '黑沼村的夜哭声', place: '黑沼村', briefing: '黑沼村连续三晚传出婴儿哭声。村民说水鬼回来了，井边却摆着新娘的红线。', monsterName: '沼泽水鬼', deepTruth: '被献祭的新娘把自己的名字藏在水草里', hiddenClue: '一枚刻着双姓的婚戒', kind: 'blood_wraith', waveBonus: 6, background: 'first' }),
            this.extend(common, { id: 'silver_pine_beast', title: '银松路的兽痕', place: '银松驿路', briefing: '商队在银松路失踪，路边都是狼爪，车厢里却没有一滴马血。', monsterName: '银松狼人', deepTruth: '被诅咒的猎人仍记得自己守过哪条路', hiddenClue: '半张被火烧过的猎人契约', kind: 'corrupted_wolf', waveBonus: 7, background: 'first' }),
            this.extend(common, { id: 'black_nail_forge', title: '铁匠铺的黑钉案', place: '灰鸦铁匠铺', briefing: '铁匠铺夜里自己打铁。第二天，砧板上多出一排还在渗血的黑钉。', monsterName: '黑钉傀儡', deepTruth: '教会失败的实验没有埋进墓地，只换了一个名字', hiddenClue: '一张没有教会印章的封钉配方', kind: 'black_nail_puppet', waveBonus: 1, background: 'second' }),
            this.extend(common, { id: 'crowbone_grave', title: '乌鸦坡的缝尸人', place: '乌鸦坡', briefing: '乱葬坡的乌鸦不再啄眼睛，只把骨头排成字。守墓人说，那些字在叫他的旧名。', monsterName: '鸦骨缝尸', deepTruth: '被缝起来的不是尸体，是一群没人愿意认领的名字', hiddenClue: '一截打结的黑羽缝线', kind: 'crowbone_stitcher', waveBonus: 4, background: 'third' })
        ];
    },

    buildPartners: function () {
        return [
            { kind: 'pyromancer', name: '赤焰术师', description: '火焰准备更强，但容易让村民害怕' },
            { kind: 'nightCrow', name: '夜鸦女术士', description: '能听出谎言，教会和胆小村民会更紧张' },
            { kind: 'hound', name: '老猎犬', description: '调查次数 +1，战斗没有直接帮助' },
            { kind: 'smith', name: '沉默铁匠', description: '银器准备更强，先收 5 金币' }
        ];
    },

    buildLoadouts: function () {
        return [
            { name: '银剑油', description: '适合怕银的怪物', weakness: 'silver', isWeaknessPrep: true, isDefense: false },
            { name: '火焰符文', description: '适合怕火的怪物', weakness: 'fire', isWeaknessPrep: true, isDefense: false },
            { name: '黑血药剂', description: '适合会吸血或入体的怪物', weakness: 'blackBlood', isWeaknessPrep: true, isDefense: false },
            { name: '鸦羽符咒', description: '适合被名字或誓言束缚的怪物', weakness: 'ravenCharm', isWeaknessPrep: true, isDefense: false },
            { name: '防御护符', description: '不知道弱点时更稳', weakness: 'silver', isWeaknessPrep: false, isDefense: true },
            { name: '女巫伙伴', description: '让已发现的真相更容易压垮怪物', weakness: 'ravenCharm', isWeaknessPrep: false, isDefense: false }
        ];
    },

    buildMainKnownFacts: function () {
        if (!this.main) {
            return '';
        }
        var flags = this.main.flags;
        var lines = [];
        lines.push('线索：');
        lines.push(flags.blackBlood ? '- 黑血：银剑能伤它' : flags.ironCorpse ? '- 围裙尸体：死者不是铁匠' : flags.cryptGate ? '- 地下门：黑蜡记得火' : '- 未确认');
        if (flags.clawMarks) { lines.push('- 爪痕：护盾有裂缝'); }
        if (flags.falseTestimony) { lines.push('- 假供词：哭声是警告'); }
        if (flags.blackenedHammer) { lines.push('- 黑血铁锤：怕火与银'); }
        if (flags.chapelWax) { lines.push('- 教堂黑蜡：银钉操控尸体'); }
        if (flags.blackWaxAltar) { lines.push('- 黑蜡祭坛：选择被封存'); }
        if (flags.sealedReliquary) { lines.push('- 圣匣：银钉刻着孩子名字'); }
        if (this.main.choiceIndex >= 0) {
            lines.push('- 已选：' + this.main.night.steps[this.main.night.steps.length - 1].labels[this.main.choiceIndex]);
        }
        return lines.join('\n');
    },

    buildKnownFacts: function () {
        var r = this.run;
        var text = '已知：\n';
        text += r.culpritKnown ? '- 真凶影子：' + r.culprit + '\n' : '- 真凶影子：未知\n';
        text += r.weaknessKnown ? '- 怪物弱点：' + this.getWeaknessName(r.weakness) + '\n' : '- 怪物弱点：未知\n';
        text += r.attitudeKnown ? '- 村民态度：' + r.attitude + '\n' : '- 村民态度：未知\n';
        text += r.weatherKnown ? '- 夜晚天气：' + r.weather + '\n' : '- 夜晚天气：未知\n';
        text += r.costKnown ? '- 结局代价：' + r.cost + '\n' : '- 结局代价：未知\n';
        if (r.clues.length > 0) {
            text += '\n线索：\n';
            var start = Math.max(0, r.clues.length - 4);
            for (var i = start; i < r.clues.length; i++) {
                text += '- ' + r.clues[i] + '\n';
            }
        }
        return text;
    },

    buildEndingResult: function (choiceIndex) {
        if (choiceIndex === 1) {
            return {
                id: 'truth',
                title: this.run.culpritKnown ? '真相结局' : '误判结局',
                text: this.run.culpritKnown
                    ? '你说出' + this.run.culprit + '的名字。' + this.run.contract.deepTruth + '没有立刻变成证词，只像一根刺留在每个人喉咙里。'
                    : '你把推断说出口，却缺了最硬的一枚钉子。' + this.run.contract.place + '有人点头，有人冷笑，真相只被掀起一角。',
                gold: 4,
                conscience: this.run.culpritKnown ? 2 : 0,
                fear: this.run.culpritKnown ? 1 : 2,
                fame: this.run.culpritKnown ? 2 : 0
            };
        }
        if (choiceIndex === 2) {
            return {
                id: 'dark',
                title: '黑暗结局',
                text: '钱袋落进掌心时，' + this.run.contract.place + '终于安静。没有人再提' + this.run.culprit + '，也没有人敢问怪物为什么学会了人的敲门声。',
                gold: 35,
                conscience: -2,
                fear: 1,
                fame: 0
            };
        }
        if (choiceIndex === 3) {
            return {
                id: 'hidden',
                title: '隐藏结局',
                text: '你放走了' + this.run.contract.monsterName + '。它没有回头，只在泥里留下' + this.run.contract.hiddenClue + '。第二天，村民把门槛洗了三遍。',
                gold: 0,
                conscience: 1,
                fear: 2,
                fame: -1
            };
        }
        return {
            id: 'bounty',
            title: '委托结局',
            text: '你按委托斩下怪物，把赏金放进袋里。' + this.run.contract.place + '亮起几盏灯，灯下的人没有互相看。',
            gold: 25,
            conscience: this.run.culprit === '怪物' ? 1 : -1,
            fear: -1,
            fame: 1
        };
    },

    showGallery: function () {
        this.mode = 'gallery';
        this.refreshHeader();
        this.setBackground('third');
        this.playMusic(this.assets.music.third);
        this.setMonsterVisual('black_moon_knight', '结局图鉴');
        this.questLabel.string = '结局收集';
        this.storyTitle.string = '结局图鉴';

        var text = '主线三晚：\n';
        if (this.mainEndings.length === 0) {
            text += '- 暂无\n';
        } else {
            for (var i = 0; i < this.mainEndings.length; i++) {
                text += '- ' + this.mainEndings[i] + '\n';
            }
        }
        text += '\n猎魔委托：\n';
        for (var c = 0; c < this.contracts.length; c++) {
            text += '- 《' + this.contracts[c].title + '》 ' + this.getContractEndingCount(this.contracts[c].id) + '/4\n';
        }
        this.storyText.string = text;
        this.hintLabel.string = '图鉴会记录不同代价，不会告诉你哪一个才是正确。';
        this.showChoices([
            { text: '猎魔委托板', action: this.showContractBoard.bind(this) },
            { text: '返回入口', action: this.showHome.bind(this) }
        ]);
    },

    unlockMainEnding: function (nightNumber, choiceIndex) {
        var labels = ['A', 'B', 'C'];
        var entry = '第' + nightNumber + '晚 - 选择' + labels[choiceIndex];
        if (this.mainEndings.indexOf(entry) < 0) {
            this.mainEndings.push(entry);
            this.saveJson('ravenbound_cocos_main_endings', this.mainEndings);
        }
    },

    unlockContractEnding: function (contractId, endingId) {
        if (!this.contractEndings[contractId]) {
            this.contractEndings[contractId] = {};
        }
        this.contractEndings[contractId][endingId] = true;
        this.saveJson('ravenbound_cocos_contract_endings', this.contractEndings);
    },

    getContractEndingCount: function (contractId) {
        var unlocked = this.contractEndings[contractId] || {};
        var ids = ['bounty', 'truth', 'dark', 'hidden'];
        var count = 0;
        for (var i = 0; i < ids.length; i++) {
            if (unlocked[ids[i]]) {
                count++;
            }
        }
        return count;
    },

    getTotalContractEndings: function () {
        var total = 0;
        for (var i = 0; i < this.contracts.length; i++) {
            total += this.getContractEndingCount(this.contracts[i].id);
        }
        return total;
    },

    buildEnemyStatus: function () {
        if (!this.battle) {
            return '';
        }
        var lines = [];
        var enemies = this.battle.enemies;
        for (var i = 0; i < enemies.length; i++) {
            var e = enemies[i];
            lines.push(e.name + ' HP ' + e.hp + '/' + e.maxHp);
        }
        var first = this.getLivingEnemies()[0];
        if (first) {
            lines.push('弱点：' + (first.weaknessKnown ? this.getWeaknessName(first.weakness) : '未确认'));
        }
        return lines.join('\n');
    },

    buildPartyStatus: function () {
        var source = this.battle ? this.battle.party : this.party;
        var lines = [];
        for (var i = 0; i < source.length; i++) {
            var u = source[i];
            lines.push(u.name + ' HP ' + u.hp + '/' + u.maxHp + ' MP ' + u.mp + '/' + u.maxMp);
        }
        return lines.join('  ');
    },

    buildLedgerLine: function () {
        return '声望：金币 ' + this.ledger.gold + '  良知 ' + this.ledger.conscience + '  恐惧 ' + this.ledger.fear + '  名声 ' + this.ledger.fame;
    },

    buildReputationPressure: function () {
        if (this.ledger.fear >= 3) {
            return '恐惧已经压进村民舌头里，询问会更难。';
        }
        if (this.ledger.conscience >= 3) {
            return '有人听说你曾经少收过钱，愿意在门后多留一盏灯。';
        }
        if (this.ledger.gold >= 60) {
            return '钱足够买好油和新银，但钱不会替你判断谁在撒谎。';
        }
        return '现在还没人知道你会成为哪一种猎魔人。';
    },

    updatePartyView: function () {
        var source = this.battle ? this.battle.party : this.party;
        for (var i = 0; i < this.partySlots.length; i++) {
            var slot = this.partySlots[i];
            var unit = source && i < source.length ? source[i] : null;
            slot.panel.active = !!unit;
            if (unit) {
                slot.nameLabel.string = unit.name;
                slot.hpLabel.string = 'HP ' + unit.hp + '/' + unit.maxHp;
                this.loadSprite(this.assets.portraits[unit.portrait || 'hunter'], slot.sprite, slot.image);
            }
        }
        this.partyStatus.string = this.buildPartyStatus();
    },

    refreshHeader: function () {
        this.titleLabel.string = 'Ravenbound Cocos - 猎魔委托';
        this.statLabel.string = '金币 ' + this.ledger.gold
            + '  良知 ' + this.ledger.conscience
            + '  恐惧 ' + this.ledger.fear
            + '  名声 ' + this.ledger.fame;
    },

    showChoices: function (choices) {
        this.clearButtons();
        this.currentChoices = choices || [];
        for (var i = 0; i < this.currentChoices.length; i++) {
            var row = Math.floor(i / 3);
            var col = i % 3;
            var width = 258;
            var x = (col - 1) * 294;
            var y = 54 - row * 58;
            this.createButton((i + 1) + '. ' + this.currentChoices[i].text, x, y, width, 52, i);
        }
    },

    choose: function (index) {
        if (!this.currentChoices || index < 0 || index >= this.currentChoices.length) {
            return;
        }
        var choice = this.currentChoices[index];
        this.playSfx(this.assets.sfx.click);
        if (choice && choice.action) {
            choice.action();
        }
    },

    clearButtons: function () {
        for (var i = 0; i < this.buttons.length; i++) {
            if (this.buttons[i] && this.buttons[i].isValid) {
                this.buttons[i].destroy();
            }
        }
        this.buttons = [];
    },

    createButton: function (text, x, y, w, h, choiceIndex) {
        var node = this.createPanel('ChoiceButton', this.buttonLayer, x, y, w, h, new cc.Color(31, 48, 38, 238));
        node.on(cc.Node.EventType.TOUCH_END, function () {
            this.choose(choiceIndex);
        }, this);
        var label = this.createLabel('ChoiceText', node, text, 17, 0, 0, w - 18, h - 8, new cc.Color(246, 240, 218, 255), cc.Label.HorizontalAlign.CENTER);
        label.verticalAlign = cc.Label.VerticalAlign.CENTER;
        this.buttons.push(node);
        return node;
    },

    createPortraitSlot: function (name, parent, x, y) {
        var panel = this.createPanel(name + 'Panel', parent, x, y, 112, 104, new cc.Color(9, 13, 13, 208));
        var image = this.createNode(name + 'Image', panel, 0, 16, 72, 58);
        var sprite = image.addComponent(cc.Sprite);
        sprite.sizeMode = cc.Sprite.SizeMode.CUSTOM;
        var nameLabel = this.createLabel(name + 'Name', panel, '', 13, 0, -24, 100, 20, new cc.Color(240, 226, 184, 255), cc.Label.HorizontalAlign.CENTER);
        var hpLabel = this.createLabel(name + 'Hp', panel, '', 12, 0, -42, 100, 18, new cc.Color(196, 205, 188, 255), cc.Label.HorizontalAlign.CENTER);
        return { panel: panel, image: image, sprite: sprite, nameLabel: nameLabel, hpLabel: hpLabel };
    },

    createNode: function (name, parent, x, y, w, h) {
        var node = new cc.Node(name);
        parent.addChild(node);
        node.setPosition(x, y);
        node.setContentSize(w || 0, h || 0);
        return node;
    },

    createPanel: function (name, parent, x, y, w, h, color) {
        var node = this.createNode(name, parent, x, y, w, h);
        var graphics = node.addComponent(cc.Graphics);
        graphics.fillColor = color;
        graphics.rect(-w / 2, -h / 2, w, h);
        graphics.fill();
        graphics.strokeColor = new cc.Color(168, 150, 105, 120);
        graphics.lineWidth = 1;
        graphics.rect(-w / 2, -h / 2, w, h);
        graphics.stroke();
        return node;
    },

    createLabel: function (name, parent, text, size, x, y, w, h, color, align) {
        var node = this.createNode(name, parent, x, y, w, h);
        var label = node.addComponent(cc.Label);
        label.string = text;
        label.fontSize = size;
        label.lineHeight = Math.ceil(size * 1.28);
        label.overflow = cc.Label.Overflow.RESIZE_HEIGHT;
        label.enableWrapText = true;
        label.horizontalAlign = align || cc.Label.HorizontalAlign.LEFT;
        label.verticalAlign = cc.Label.VerticalAlign.CENTER;
        node.color = color || cc.Color.WHITE;
        return label;
    },

    setBackground: function (key) {
        var path = this.assets.backgrounds[key] || this.assets.backgrounds.first;
        this.loadSprite(path, this.backgroundSprite, this.background);
    },

    setMonsterVisual: function (kind, label) {
        this.monsterName.string = label || '';
        var path = this.assets.monsters[kind] || this.assets.monsters.corrupted_wolf;
        this.loadSprite(path, this.monsterSprite, this.monsterNode);
    },

    loadSprite: function (path, sprite, node) {
        this.loadResource(path, cc.SpriteFrame, function (err, frame) {
            if (!err && frame && sprite && sprite.node && sprite.node.isValid) {
                sprite.spriteFrame = frame;
                node.setContentSize(node.width || 200, node.height || 120);
                return;
            }
            this.drawFallback(node, path);
        }.bind(this));
    },

    drawFallback: function (node, label) {
        if (!node || !node.isValid) {
            return;
        }
        var graphics = node.getComponent(cc.Graphics) || node.addComponent(cc.Graphics);
        graphics.clear();
        graphics.fillColor = new cc.Color(24, 28, 30, 255);
        graphics.rect(-node.width / 2, -node.height / 2, node.width, node.height);
        graphics.fill();
        this.createLabel('MissingAsset', node, label, 12, 0, 0, node.width - 10, 30, new cc.Color(170, 170, 160, 255), cc.Label.HorizontalAlign.CENTER);
    },

    playNightTransition: function (title, subtitle, onDone) {
        this.transitionLayer.active = true;
        this.transitionLayer.opacity = 0;
        this.transitionTitle.string = title;
        this.transitionSubtitle.string = subtitle;
        this.playSfx(this.assets.sfx.transition);
        this.transitionLayer.stopAllActions();
        this.transitionLayer.runAction(cc.sequence(
            cc.fadeIn(0.28),
            cc.delayTime(0.62),
            cc.callFunc(function () {
                if (onDone) {
                    onDone();
                }
            }),
            cc.delayTime(0.15),
            cc.fadeOut(0.28),
            cc.callFunc(function () {
                this.transitionLayer.active = false;
            }.bind(this))
        ));
    },

    playMusic: function (path) {
        if (this.musicPath === path) {
            return;
        }
        this.musicPath = path;
        this.loadResource(path, cc.AudioClip, function (err, clip) {
            if (err || !clip) {
                return;
            }
            cc.audioEngine.stopMusic();
            cc.audioEngine.setMusicVolume(this.musicVolume);
            cc.audioEngine.playMusic(clip, true);
        }.bind(this));
    },

    playSfx: function (path) {
        this.loadResource(path, cc.AudioClip, function (err, clip) {
            if (!err && clip) {
                cc.audioEngine.playEffect(clip, false);
                cc.audioEngine.setEffectsVolume(this.sfxVolume);
            }
        }.bind(this));
    },

    playSkillSfx: function (skill) {
        if (!skill) {
            return;
        }
        if (skill.animation === 'slash') {
            this.playSfx(this.assets.sfx.sword);
        } else if (skill.animation === 'flame' || skill.animation === 'cast') {
            this.playSfx(this.assets.sfx.fire);
        } else {
            this.playSfx(this.assets.sfx.confirm);
        }
    },

    loadResource: function (path, type, callback) {
        if (cc.resources && cc.resources.load) {
            cc.resources.load(path, type, callback);
            return;
        }
        cc.loader.loadRes(path, type, callback);
    },

    loadLedger: function () {
        return {
            gold: this.loadInt('ravenbound_cocos_gold', 20),
            conscience: this.loadInt('ravenbound_cocos_conscience', 0),
            fear: this.loadInt('ravenbound_cocos_fear', 0),
            fame: this.loadInt('ravenbound_cocos_fame', 0)
        };
    },

    saveLedger: function () {
        this.saveInt('ravenbound_cocos_gold', this.ledger.gold);
        this.saveInt('ravenbound_cocos_conscience', this.ledger.conscience);
        this.saveInt('ravenbound_cocos_fear', this.ledger.fear);
        this.saveInt('ravenbound_cocos_fame', this.ledger.fame);
    },

    loadInt: function (key, fallback) {
        try {
            var value = cc.sys.localStorage.getItem(key);
            if (value === null || value === undefined || value === '') {
                return fallback;
            }
            return parseInt(value, 10);
        } catch (e) {
            return fallback;
        }
    },

    saveInt: function (key, value) {
        try {
            cc.sys.localStorage.setItem(key, String(value));
        } catch (e) {
        }
    },

    loadJson: function (key, fallback) {
        try {
            var raw = cc.sys.localStorage.getItem(key);
            return raw ? JSON.parse(raw) : fallback;
        } catch (e) {
            return fallback;
        }
    },

    saveJson: function (key, value) {
        try {
            cc.sys.localStorage.setItem(key, JSON.stringify(value));
        } catch (e) {
        }
    },

    getWeaknessName: function (weakness) {
        switch (weakness) {
            case 'fire':
                return '火焰';
            case 'blackBlood':
                return '黑血药剂';
            case 'ravenCharm':
                return '鸦羽符咒';
            default:
                return '银剑';
        }
    },

    pick: function (items) {
        return items[Math.floor(Math.random() * items.length)];
    },

    signed: function (value) {
        return value >= 0 ? '+' + value : String(value);
    },

    clone: function (value) {
        return JSON.parse(JSON.stringify(value));
    },

    extend: function (base, extra) {
        var result = {};
        var key;
        for (key in base) {
            if (base.hasOwnProperty(key)) {
                result[key] = base[key];
            }
        }
        for (key in extra) {
            if (extra.hasOwnProperty(key)) {
                result[key] = extra[key];
            }
        }
        return result;
    }
});
