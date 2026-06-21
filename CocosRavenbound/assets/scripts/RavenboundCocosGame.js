cc.Class({
    name: 'RavenboundCocosGame',
    extends: cc.Component,

    onLoad: function () {
        this.width = 960;
        this.height = 640;
        this.musicPath = '';
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
                wolf: 'art/monsters/corrupted_wolf',
                wraith: 'art/monsters/blood_wraith',
                puppet: 'art/monsters/black_nail_puppet',
                knight: 'art/monsters/black_moon_knight'
            },
            music: {
                map: 'music/first_night',
                battle: 'music/battle'
            },
            sfx: {
                click: 'sfx/ui_click',
                sword: 'sfx/sword_hit',
                fire: 'sfx/fire_cast',
                victory: 'sfx/battle_victory',
                transition: 'sfx/night_transition'
            }
        };

        this.contractTemplates = [
            {
                title: '黑沼村的夜哭声',
                place: '黑沼村',
                background: 'first',
                opening: '村民说井口每到夜半就有孩子哭。猎魔人被请来时，村口的灯已经全灭了。',
                monsters: [
                    { id: 'wraith', name: '血怨灵', hp: 86, attack: 13 },
                    { id: 'puppet', name: '黑钉傀儡', hp: 104, attack: 15 },
                    { id: 'wolf', name: '腐化狼', hp: 78, attack: 12 }
                ]
            },
            {
                title: '黑钉锻炉的委托',
                place: '乌鸦锻炉',
                background: 'second',
                opening: '铁匠铺一夜之间多出十三枚黑钉，钉帽上都刻着同一个人的名字。',
                monsters: [
                    { id: 'puppet', name: '黑钉傀儡', hp: 112, attack: 16 },
                    { id: 'wolf', name: '腐化狼', hp: 88, attack: 13 }
                ]
            },
            {
                title: '黑蜡礼拜堂',
                place: '黑蜡墓堂',
                background: 'third',
                opening: '特莉丝在墓堂门口等你。黑蜡顺着圣像流下，像有人把夜色熬成了油。',
                monsters: [
                    { id: 'knight', name: '黑月骑士', hp: 130, attack: 18 },
                    { id: 'wraith', name: '血怨灵', hp: 96, attack: 14 }
                ],
                triss: true
            }
        ];

        this.truths = ['村长', '女巫', '怪物', '被害人自己'];
        this.weaknesses = ['银剑', '火焰', '黑血药剂', '鸦羽符咒'];
        this.attitudes = ['隐瞒', '求救', '欺骗', '出卖你'];
        this.weather = ['大雾', '暴雨', '血月', '无月夜'];
        this.costs = ['救孩子', '保村子', '拿赏金', '放走怪物'];

        this.stats = {
            gold: 20,
            conscience: 3,
            fear: 0,
            fame: 1
        };
        this.gallery = this.loadGallery();
        this.buildView();
        this.startRun();
    },

    onDestroy: function () {
        cc.audioEngine.stopMusic();
    },

    buildView: function () {
        this.node.setContentSize(this.width, this.height);
        this.background = this.createNode('Background', this.node, 0, 0, this.width, this.height);
        this.background.zIndex = -20;
        this.backgroundSprite = this.background.addComponent(cc.Sprite);
        this.backgroundSprite.sizeMode = cc.Sprite.SizeMode.CUSTOM;

        this.tint = this.createPanel('Tint', this.node, 0, 0, this.width, this.height, new cc.Color(0, 0, 0, 98));
        this.tint.zIndex = -10;

        this.titleLabel = this.createLabel('Title', this.node, '', 30, -410, 278, 520, 44, new cc.Color(244, 236, 209, 255), cc.Label.HorizontalAlign.LEFT);
        this.statLabel = this.createLabel('Stats', this.node, '', 18, 178, 286, 520, 32, new cc.Color(221, 214, 186, 255), cc.Label.HorizontalAlign.RIGHT);

        this.storyPanel = this.createPanel('StoryPanel', this.node, -170, 58, 580, 270, new cc.Color(11, 16, 17, 210));
        this.storyTitle = this.createLabel('StoryTitle', this.storyPanel, '', 22, 0, 100, 520, 32, new cc.Color(255, 227, 149, 255), cc.Label.HorizontalAlign.LEFT);
        this.storyText = this.createLabel('StoryText', this.storyPanel, '', 19, 0, 12, 520, 160, new cc.Color(235, 238, 226, 255), cc.Label.HorizontalAlign.LEFT);
        this.hintLabel = this.createLabel('Hint', this.storyPanel, '', 15, 0, -108, 520, 28, new cc.Color(168, 180, 166, 255), cc.Label.HorizontalAlign.LEFT);

        this.visualPanel = this.createPanel('VisualPanel', this.node, 310, 64, 270, 270, new cc.Color(7, 10, 12, 196));
        this.monsterNode = this.createNode('Monster', this.visualPanel, 0, 22, 210, 160);
        this.monsterSprite = this.monsterNode.addComponent(cc.Sprite);
        this.monsterSprite.sizeMode = cc.Sprite.SizeMode.CUSTOM;
        this.monsterName = this.createLabel('MonsterName', this.visualPanel, '', 18, 0, -104, 230, 28, new cc.Color(222, 216, 191, 255), cc.Label.HorizontalAlign.CENTER);

        this.portraitRow = this.createNode('PortraitRow', this.node, -294, -180, 360, 110);
        this.hunterPortrait = this.createPortrait('HunterPortrait', this.portraitRow, -72, 0, 'hunter');
        this.trissPortrait = this.createPortrait('TrissPortrait', this.portraitRow, 72, 0, 'triss');

        this.buttonLayer = this.createNode('Buttons', this.node, 0, -250, 860, 150);
        this.buttons = [];
    },

    startRun: function () {
        var template = this.pick(this.contractTemplates);
        var monster = this.pick(template.monsters);
        this.contract = {
            template: template,
            title: template.title,
            place: template.place,
            background: template.background,
            opening: template.opening,
            trueCulprit: this.pick(this.truths),
            weakness: this.pick(this.weaknesses),
            attitude: this.pick(this.attitudes),
            weather: this.pick(this.weather),
            cost: this.pick(this.costs),
            monster: {
                id: monster.id,
                name: monster.name,
                maxHp: monster.hp,
                hp: monster.hp,
                attack: monster.attack
            },
            triss: !!template.triss
        };
        this.player = {
            hp: 112,
            maxHp: 112,
            guard: false
        };
        this.triss = {
            hp: 82,
            maxHp: 82,
            ready: this.contract.triss
        };
        this.investigationsLeft = 3;
        this.clues = [];
        this.prep = null;
        this.revealed = {};
        this.phase = 'briefing';
        this.playMusic(this.assets.music.map);
        this.setBackground(this.contract.background);
        this.loadSprite(this.assets.monsters[this.contract.monster.id], this.monsterSprite, this.monsterNode);
        this.showBriefing();
    },

    showBriefing: function () {
        this.refreshHeader();
        this.monsterName.string = '委托目标：未确认';
        this.storyTitle.string = this.contract.title;
        this.storyText.string = this.contract.opening + '\n\n你只有三次调查机会。每个选择都可能让真相更清楚，也可能让你带着错误的判断进战斗。';
        this.hintLabel.string = '核心：有限调查，不完整信息，最后承担选择。';
        this.showChoices([
            { text: '接下委托', action: this.showInvestigation.bind(this) },
            { text: '换一个委托', action: this.startRun.bind(this) },
            { text: '查看结局图鉴', action: this.showGallery.bind(this) }
        ]);
    },

    showInvestigation: function () {
        this.phase = 'investigation';
        this.refreshHeader();
        this.monsterName.string = '阴影里有东西在等';
        this.storyTitle.string = '调查：' + this.contract.place;
        this.storyText.string = this.describeClues();
        this.hintLabel.string = '剩余调查次数：' + this.investigationsLeft + '。线索不会全部给你。';

        if (this.investigationsLeft <= 0) {
            this.showPreparation();
            return;
        }

        this.showChoices([
            { text: '调查尸体', action: this.takeClue.bind(this, 'body') },
            { text: '询问村长', action: this.takeClue.bind(this, 'chief') },
            { text: '去教堂', action: this.takeClue.bind(this, 'chapel') },
            { text: '跟踪寡妇', action: this.takeClue.bind(this, 'widow') },
            { text: '查看井口', action: this.takeClue.bind(this, 'well') },
            { text: '检查脚印', action: this.takeClue.bind(this, 'tracks') },
            { text: '停止调查', action: this.showPreparation.bind(this) }
        ]);
    },

    takeClue: function (kind) {
        if (this.clues.indexOf(kind) >= 0) {
            return;
        }

        this.playSfx(this.assets.sfx.click);
        this.investigationsLeft -= 1;
        var clue = this.getClue(kind);
        this.clues.push(kind);
        this.lastClue = clue;
        this.storyTitle.string = '新线索';
        this.storyText.string = clue + '\n\n' + this.describeClues();
        this.hintLabel.string = '剩余调查次数：' + this.investigationsLeft;

        if (this.investigationsLeft <= 0) {
            this.showChoices([
                { text: '整理线索', action: this.showPreparation.bind(this) }
            ]);
            return;
        }

        this.showChoices([
            { text: '继续调查', action: this.showInvestigation.bind(this) },
            { text: '现在准备战斗', action: this.showPreparation.bind(this) }
        ]);
    },

    getClue: function (kind) {
        switch (kind) {
            case 'body':
                this.revealed.monster = true;
                return '尸体没有挣扎痕迹，伤口像被仪式压出来的。目标很可能是' + this.contract.monster.name + '。';
            case 'chief':
                this.revealed.culprit = this.contract.attitude !== '欺骗';
                return this.contract.attitude === '欺骗'
                    ? '村长说得太顺了。你知道他在撒谎，但还不知道为了谁。'
                    : '村长的手在发抖。他漏出一句：真正想让委托结束的人是' + this.contract.trueCulprit + '。';
            case 'chapel':
                this.revealed.cost = true;
                return '教堂蜡台下压着一枚旧吊坠。解决这件事最可能牵扯到：' + this.contract.cost + '。';
            case 'widow':
                this.revealed.attitude = true;
                return '寡妇没有哭。她只说村民这次选择了' + this.contract.attitude + '。';
            case 'well':
                this.revealed.weakness = true;
                return '井壁有焦黑和银粉，痕迹指向弱点：' + this.contract.weakness + '。';
            case 'tracks':
                this.revealed.weather = true;
                return '脚印在村外断掉。今夜是' + this.contract.weather + '，怪物会借天气藏身。';
            default:
                return '你什么都没查到，只有雾更重了。';
        }
    },

    describeClues: function () {
        var text = '已知信息：\n';
        text += '真凶：' + (this.revealed.culprit ? this.contract.trueCulprit : '不确定') + '\n';
        text += '怪物：' + (this.revealed.monster ? this.contract.monster.name : '不确定') + '\n';
        text += '弱点：' + (this.revealed.weakness ? this.contract.weakness : '不确定') + '\n';
        text += '村民态度：' + (this.revealed.attitude ? this.contract.attitude : '不确定') + '\n';
        text += '天气：' + (this.revealed.weather ? this.contract.weather : '不确定') + '\n';
        text += '代价：' + (this.revealed.cost ? this.contract.cost : '不确定');
        return text;
    },

    showPreparation: function () {
        this.phase = 'preparation';
        this.refreshHeader();
        this.storyTitle.string = '战前准备';
        this.storyText.string = '你把线索摊开。调查越准，战斗越轻。选错也能赢，只是会疼。';
        this.hintLabel.string = '真正的考验不是操作，是你相信哪条线索。';
        this.monsterName.string = this.contract.monster.name + '  HP ' + this.contract.monster.hp + '/' + this.contract.monster.maxHp;

        var choices = [
            { text: '涂银剑油', action: this.choosePreparation.bind(this, '银剑') },
            { text: '刻火焰符文', action: this.choosePreparation.bind(this, '火焰') },
            { text: '饮黑血药剂', action: this.choosePreparation.bind(this, '黑血药剂') },
            { text: '带鸦羽符咒', action: this.choosePreparation.bind(this, '鸦羽符咒') }
        ];
        if (this.contract.triss) {
            choices.push({ text: '让特莉丝同行', action: this.choosePreparation.bind(this, '特莉丝') });
        }
        this.showChoices(choices);
    },

    choosePreparation: function (prep) {
        this.prep = prep;
        if (prep === '特莉丝') {
            this.triss.ready = true;
        }
        this.playSfx(prep === '火焰' || prep === '特莉丝' ? this.assets.sfx.fire : this.assets.sfx.click);
        this.startBattle();
    },

    startBattle: function () {
        this.phase = 'battle';
        this.setBackground('battle');
        this.playMusic(this.assets.music.battle);
        this.refreshHeader();
        this.storyTitle.string = '战斗';
        this.storyText.string = '你选择了：' + this.prep + '。\n' + this.contract.monster.name + '从阴影里站起来。';
        this.hintLabel.string = this.prep === this.contract.weakness ? '弱点命中：战斗会轻很多。' : '弱点未确认：战斗会更危险。';
        this.updateBattleText();
        this.showBattleChoices();
    },

    showBattleChoices: function () {
        var choices = [
            { text: '银剑斩击', action: this.playerAction.bind(this, 'slash') },
            { text: '法印', action: this.playerAction.bind(this, 'sign') },
            { text: '防御', action: this.playerAction.bind(this, 'guard') }
        ];
        if (this.triss.ready && this.triss.hp > 0) {
            choices.push({ text: '特莉丝：火矢', action: this.playerAction.bind(this, 'triss') });
        }
        this.showChoices(choices);
    },

    playerAction: function (action) {
        var damage = 0;
        this.player.guard = false;
        if (action === 'slash') {
            damage = 18 + (this.prep === '银剑' ? 18 : 0);
            this.playSfx(this.assets.sfx.sword);
        } else if (action === 'sign') {
            damage = 16 + (this.prep === '火焰' ? 20 : 0);
            this.playSfx(this.assets.sfx.fire);
        } else if (action === 'guard') {
            this.player.guard = true;
            this.storyText.string = '你压低身体，银剑横在胸口。怪物的下一击会被削弱。';
            this.enemyTurn();
            return;
        } else if (action === 'triss') {
            damage = 26 + (this.contract.weakness === '火焰' ? 12 : 0);
            this.playSfx(this.assets.sfx.fire);
        }

        if (this.prep === this.contract.weakness && action !== 'guard') {
            damage += 10;
        }
        if (this.prep === '黑血药剂' && this.contract.weakness === '黑血药剂') {
            damage += 12;
        }
        if (this.prep === '鸦羽符咒' && this.contract.weakness === '鸦羽符咒') {
            damage += 12;
        }

        this.contract.monster.hp = Math.max(0, this.contract.monster.hp - damage);
        this.storyText.string = '你造成 ' + damage + ' 点伤害。';
        this.updateBattleText();

        if (this.contract.monster.hp <= 0) {
            this.scheduleOnce(function () {
                this.showJudgement();
            }.bind(this), 0.45);
            return;
        }

        this.enemyTurn();
    },

    enemyTurn: function () {
        var damage = this.contract.monster.attack;
        if (this.player.guard) {
            damage = Math.ceil(damage * 0.45);
        }
        if (this.prep === '黑血药剂' && this.contract.weakness === '黑血药剂') {
            damage = Math.max(3, damage - 5);
        }
        if (this.triss.ready && this.triss.hp > 0 && Math.random() < 0.35) {
            this.triss.hp = Math.max(0, this.triss.hp - Math.ceil(damage * 0.65));
            this.storyText.string += '\n怪物扑向特莉丝，她替你扛下一半黑影。';
        } else {
            this.player.hp = Math.max(0, this.player.hp - damage);
            this.storyText.string += '\n' + this.contract.monster.name + '反击，造成 ' + damage + ' 点伤害。';
        }
        this.updateBattleText();

        if (this.player.hp <= 0) {
            this.showEnding('悲剧结局', '你倒在村口，第二天村民说猎魔人也会流血。真相没有被带出黑夜。', { fear: 2, fame: -1 });
            return;
        }

        this.showBattleChoices();
    },

    updateBattleText: function () {
        this.monsterName.string = this.contract.monster.name + '  HP ' + this.contract.monster.hp + '/' + this.contract.monster.maxHp;
        this.hintLabel.string = '猎魔人 HP ' + this.player.hp + '/' + this.player.maxHp
            + (this.triss.ready ? '  特莉丝 HP ' + this.triss.hp + '/' + this.triss.maxHp : '')
            + '  弱点：' + (this.revealed.weakness ? this.contract.weakness : '未确认');
    },

    showJudgement: function () {
        this.phase = 'judgement';
        this.playMusic(this.assets.music.map);
        this.setBackground(this.contract.background);
        this.refreshHeader();
        this.storyTitle.string = '审判真相';
        this.storyText.string = '怪物倒下了，但村子没有立刻变亮。\n你掌握的真相并不完整，选择会留下代价。';
        this.hintLabel.string = '没有善恶按钮，只有你愿意背下来的后果。';
        this.showChoices([
            { text: '杀死怪物，领赏离开', action: this.endKill.bind(this) },
            { text: '揭穿' + this.contract.trueCulprit, action: this.endReveal.bind(this) },
            { text: '收钱沉默', action: this.endSilence.bind(this) },
            { text: '放走怪物，追查幕后', action: this.endRelease.bind(this) },
            { text: '牺牲报酬去' + this.contract.cost, action: this.endCost.bind(this) }
        ]);
    },

    endKill: function () {
        this.showEnding('普通结局', '村子安全了。至少他们这样说。井口被木板钉死，哭声再也没有传出来。', { gold: 16, fame: 1, conscience: -1 });
    },

    endReveal: function () {
        var solved = this.revealed.culprit;
        this.showEnding(
            solved ? '真相结局' : '误判结局',
            solved
                ? '你把' + this.contract.trueCulprit + '推到众人面前。村子没有感谢你，但有些门从此不再锁上。'
                : '你说出了一个名字。村民沉默得太快，你知道自己缺了一块证据。',
            solved ? { fame: 2, conscience: 1, gold: -4 } : { fear: 1, fame: -1, conscience: -1 }
        );
    },

    endSilence: function () {
        this.showEnding('黑暗结局', '钱袋很重，回程很安静。你没有回头看村口那盏灯。', { gold: 26, conscience: -2, fear: 1 });
    },

    endRelease: function () {
        this.showEnding('隐藏结局', '你放走了怪物，也放走了一条通向更深处的线。村民会恨你，但某个夜晚会有人因此活下来。', { fear: 2, conscience: 1, fame: -1 });
    },

    endCost: function () {
        this.showEnding('代价结局', '你选择' + this.contract.cost + '。这不是最干净的结局，但你愿意记住它。', { gold: -10, conscience: 2, fame: 1 });
    },

    showEnding: function (name, text, delta) {
        this.applyDelta(delta || {});
        this.saveEnding(name);
        this.refreshHeader();
        this.storyTitle.string = name;
        this.storyText.string = text + '\n\n已收集结局：' + this.gallery.length + '。';
        this.hintLabel.string = '有些道理不会写在任务日志里。';
        this.playSfx(name === '普通结局' || name === '真相结局' || name === '代价结局' ? this.assets.sfx.victory : this.assets.sfx.transition);
        this.showChoices([
            { text: '再接一个委托', action: this.startRun.bind(this) },
            { text: '查看结局图鉴', action: this.showGallery.bind(this) }
        ]);
    },

    showGallery: function () {
        this.refreshHeader();
        this.storyTitle.string = '结局图鉴';
        if (this.gallery.length === 0) {
            this.storyText.string = '还没有结局。接下一份委托，给这个世界留下一道痕迹。';
        } else {
            this.storyText.string = this.gallery.join('\n');
        }
        this.hintLabel.string = '同一个委托，不同调查和审判会留下不同故事。';
        this.showChoices([
            { text: '开始委托', action: this.startRun.bind(this) }
        ]);
    },

    applyDelta: function (delta) {
        this.stats.gold = Math.max(0, this.stats.gold + (delta.gold || 0));
        this.stats.conscience = Math.max(0, this.stats.conscience + (delta.conscience || 0));
        this.stats.fear = Math.max(0, this.stats.fear + (delta.fear || 0));
        this.stats.fame = Math.max(0, this.stats.fame + (delta.fame || 0));
    },

    refreshHeader: function () {
        this.titleLabel.string = 'Ravenbound Cocos - 猎魔委托';
        this.statLabel.string = '金币 ' + this.stats.gold
            + '  良知 ' + this.stats.conscience
            + '  恐惧 ' + this.stats.fear
            + '  名声 ' + this.stats.fame;
    },

    showChoices: function (choices) {
        this.clearButtons();
        var count = choices.length;
        for (var i = 0; i < count; i++) {
            var row = Math.floor(i / 3);
            var col = i % 3;
            var width = count <= 3 ? 250 : 250;
            var x = (col - 1) * 286;
            var y = 54 - row * 58;
            this.createButton(choices[i].text, x, y, width, 52, choices[i].action);
        }
    },

    clearButtons: function () {
        for (var i = 0; i < this.buttons.length; i++) {
            this.buttons[i].destroy();
        }
        this.buttons = [];
    },

    createButton: function (text, x, y, w, h, callback) {
        var node = this.createPanel('ChoiceButton', this.buttonLayer, x, y, w, h, new cc.Color(31, 48, 38, 235));
        node.setContentSize(w, h);
        node.on(cc.Node.EventType.TOUCH_END, function () {
            this.playSfx(this.assets.sfx.click);
            callback();
        }, this);

        var label = this.createLabel('ChoiceText', node, text, 18, 0, 0, w - 24, h - 8, new cc.Color(246, 240, 218, 255), cc.Label.HorizontalAlign.CENTER);
        label.verticalAlign = cc.Label.VerticalAlign.CENTER;
        this.buttons.push(node);
        return node;
    },

    createPortrait: function (name, parent, x, y, pathKey) {
        var panel = this.createPanel(name + 'Panel', parent, x, y, 96, 96, new cc.Color(9, 13, 13, 205));
        var imageNode = this.createNode(name, panel, 0, 0, 84, 84);
        var sprite = imageNode.addComponent(cc.Sprite);
        sprite.sizeMode = cc.Sprite.SizeMode.CUSTOM;
        this.loadSprite(this.assets.portraits[pathKey], sprite, imageNode);
        return imageNode;
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
        graphics.strokeColor = new cc.Color(176, 166, 125, 120);
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
            cc.audioEngine.setMusicVolume(0.62);
            cc.audioEngine.playMusic(clip, true);
        });
    },

    playSfx: function (path) {
        this.loadResource(path, cc.AudioClip, function (err, clip) {
            if (!err && clip) {
                cc.audioEngine.playEffect(clip, false);
            }
        });
    },

    loadResource: function (path, type, callback) {
        if (cc.resources && cc.resources.load) {
            cc.resources.load(path, type, callback);
            return;
        }
        cc.loader.loadRes(path, type, callback);
    },

    pick: function (items) {
        return items[Math.floor(Math.random() * items.length)];
    },

    loadGallery: function () {
        try {
            var raw = cc.sys.localStorage.getItem('ravenbound_cocos_endings');
            return raw ? JSON.parse(raw) : [];
        } catch (e) {
            return [];
        }
    },

    saveEnding: function (name) {
        var entry = this.contract.title + ' - ' + name;
        if (this.gallery.indexOf(entry) < 0) {
            this.gallery.push(entry);
        }
        cc.sys.localStorage.setItem('ravenbound_cocos_endings', JSON.stringify(this.gallery));
    }
});
