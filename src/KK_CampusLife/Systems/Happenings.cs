using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Joojkk.CampusLife
{
    /// <summary>Small everyday moments that pop up while walking around school.</summary>
    internal static class Happenings
    {
        private static CampusState S { get { return Campus.S; } }

        private static readonly string[] All =
        {
            "coin", "pop_quiz", "carry_boxes", "love_letter", "locker_note", "dropped_notebooks", "gossip",
            "vending", "wind", "rumor", "inspection", "stray_cat", "rain", "drenched",
        };

        public static string Name(string id)
        {
            switch (id)
            {
                case "coin": return "Something shiny on the floor";
                case "pop_quiz": return "Pop quiz!";
                case "carry_boxes": return "A professor needs help";
                case "love_letter": return "A letter in your locker";
                case "locker_note": return "A note in your locker";
                case "dropped_notebooks": return "Notebooks everywhere";
                case "gossip": return "Hallway gossip";
                case "vending": return "Vending machine lottery";
                case "wind": return "A sudden gust of wind";
                case "rumor": return "A rumor about you";
                case "inspection": return "Student union inspection";
                case "stray_cat": return "A stray cat";
                case "rain": return "Sudden rain";
                case "drenched": return "Caught in the rain";
                default: return id;
            }
        }

        public static string Roll()
        {
            // The rain's events only when it rains (Weather): stuck at the entrance after school, soaked any other time.
            var pool = All.Where(id => id == "rain" ? Weather.Raining && G.CurrentMask == PeriodMask.After : id == "drenched" ? Weather.Raining && G.CurrentMask != PeriodMask.After
                                       : id != "wind" || !Weather.Raining).ToList();
            // The ones that bring a request (a letter, a note, a rumor to check) come more often, much more with the board
            // empty.
            var empty = Board.Empty;
            var w = pool.Select(id => TaskAdders.Contains(id) ? (empty ? 6f : 2.5f) : 1f).ToList();
            var roll = Random.value * w.Sum();
            for (var i = 0; i < pool.Count; i++) { roll -= w[i]; if (roll <= 0f) return pool[i]; }
            return pool[pool.Count - 1];
        }

        private static readonly string[] TaskAdders = { "love_letter", "locker_note", "rumor" };

        /// <summary>A girl for the scene: one on your map first, then anyone (weighted by attention).</summary>
        private static SaveData.Heroine Girl(System.Func<SaveData.Heroine, bool> filter = null)
        {
            var list = Board.Students.Where(h => Truth.AtSchool(h) && (filter == null || filter(h))).ToList(); // never a girl who is not here today
            if (list.Count == 0) return null;
            var here = list.Where(h => GirlInfo.CurrentMap(h) == G.PlayerMapNo && GirlInfo.IsSpawned(h)).ToList();
            var from = here.Count > 0 && Random.value < 0.7f ? here : list;
            return from[Random.Range(0, from.Count)];
        }

        /// <summary>
        /// notebooks played for a girl on another map): your map, spawned, within ~15 m; none, no scene.
        /// </summary>
        private static SaveData.Heroine Near()
        {
            var near = Conversation.Onlookers(null, 15f).Where(h => Board.Students.Contains(h) && Truth.AtSchool(h)).ToList();
            return near.Count == 0 ? null : near[Random.Range(0, System.Math.Min(3, near.Count))];
        }

        private static void Pay(Rewards r, SaveData.Heroine h, string why)
        {
            var a = Campus.Apply(r, h, false);
            var msg = why + (a.Describe() == "-" ? "" : " (" + a.Describe() + ")");
            S.Log(msg);
            Toasts.Show(msg);
        }

        public static bool Play(string id)
        {
            var b = new SceneBuilder(null, false);
            int roll;
            switch (id)
            {
                case "coin":
                    b.Narr("Something glints on the hallway floor... a 500 yen coin.");
                    b.Choice("Pocket it.", "keep", "Turn it in at the faculty room.", "turn");
                    b.Branch("keep"); b.Narr("Finders keepers."); b.End();
                    b.Branch("turn"); b.Narr("The professor thanks you for your honesty."); b.End();
                    return Scenes.PlayOnMap(b, c =>
                    {
                        if (c == "turn") Pay(new Rewards { Rep = 2 }, null, "Honest student");
                        else Pay(new Rewards { Money = 500 }, null, "Lucky find");
                    });

                case "pop_quiz":
                {
                    var ok = Campus.Check(G.Int, 50, out roll);
                    b.Narr("A professor stops you in the hallway: \"You! What's the capital of Australia? Quick!\"");
                    b.Narr(ok ? "You answer without hesitation. The professor nods, impressed." : "Your mind goes blank. \"...Sydney?\" The professor sighs.");
                    return Scenes.PlayOnMap(b, _ =>
                    {
                        if (ok) Pay(new Rewards { Rep = 2, Int = 1 }, null, "Pop quiz: correct!");
                        else Pay(new Rewards { Rep = -1 }, null, "Pop quiz: wrong answer");
                    });
                }

                case "carry_boxes":
                {
                    b.Narr("A professor is struggling with a tower of boxes full of handouts.");
                    var opts = new List<string> { "Help carry some.", "help", "Pretend you didn't see.", "ignore" };
                    if (G.Str >= 30) { opts.Add("Carry them all yourself. [STR]"); opts.Add("all"); }
                    b.Choice(opts.ToArray());
                    b.Branch("help"); b.Narr("You take half the boxes. The professor thanks you."); b.End();
                    b.Branch("ignore"); b.Narr("You suddenly find the ceiling very interesting."); b.End();
                    b.Branch("all"); b.Narr("You lift the whole tower in one go. A few students clap."); b.End();
                    return Scenes.PlayOnMap(b, c =>
                    {
                        if (c == "all") { Pay(new Rewards { Rep = 4, Str = 1 }, null, "Strong and helpful"); G.AddPeriodSeconds(40f); }
                        else if (c == "help") { Pay(new Rewards { Rep = 2 }, null, "You helped a professor"); G.AddPeriodSeconds(60f); }
                    });
                }

                case "love_letter":
                {
                    // Only a friend writes you a love letter; with nobody like that, no letter (never a stranger's).
                    var g = Girl(h => h.favor >= 50 && !h.isGirlfriend && Duo.YourFriend(h) && Truth.AtSchool(h));
                    if (g == null) return false;
                    b.Narr("There's a pink envelope in your locker. Inside: \"Please come to the rooftop after class. I have something to tell you.\"");
                    b.Narr("It's signed with a little heart. No name.");
                    return Scenes.PlayOnMap(b, _ =>
                    {
                        var r = Board.Make(RequestKind.MeetAt, g, "The love letter", "Someone wants to meet you on the rooftop after class. Who could it be? (Go there and she will come.)", 8, PeriodMask.After, lewd: 3, rep: 0);
                        r.Place = "rooftop";
                        r.Source = "happening";
                        r.Title = "A love letter";
                        Board.Add(r);
                        Toasts.Show("New request: go to the rooftop after class.");
                    });
                }

                case "locker_note":
                {
                    // Only a friend leaves a note.
                    var g = Girl(h => h.favor >= 30 && Duo.YourFriend(h) && Truth.AtSchool(h));
                    if (g == null) return false;
                    var initial = GirlInfo.FirstName(g).Substring(0, 1).ToUpper();
                    b.Narr("There's a folded note in your locker: \"Could you come to the rooftop after class? I need help with something and I'd rather not ask in front of everyone.\"");
                    b.Narr("It's signed with a single letter: " + initial + ".");
                    return Scenes.PlayOnMap(b, _ =>
                    {
                        var r = Board.Make(RequestKind.MeetAt, g, "A note in your locker",
                            "Someone signed '" + initial + ".' asked for help on the rooftop after class. Go there and she will come.",
                            6, PeriodMask.After, rep: 1);
                        r.Place = "rooftop";
                        r.Source = "note";
                        Board.Add(r);
                        Toasts.Show("New request: go to the rooftop after class.");
                    });
                }

                case "dropped_notebooks":
                {
                    var g = Near();
                    if (g == null) return false;
                    var name = GirlInfo.FirstName(g);
                    b.Narr("Someone bumps into " + name + " and her notebooks scatter across the floor.");
                    b.Say(g, Lines.T(GirlInfo.ToneOf(g), "Ah... my notes... || Oh no... my notes... || Ah... everything's falling... || Wah... my papers...", "Nooo! My stuff! || Noooo! Everything's everywhere! || Gah! My stuff! || Argh! Not again!", "...Great. || ...Wonderful. || ...Of course. || ...Typical.", "Ugh. Perfect timing. || Ugh. Great. || Lovely. Just lovely. || Well, that's annoying."));
                    b.Choice("Help her pick them up.", "help", "Keep walking.", "walk");
                    b.Branch("help");
                    b.Narr("You gather everything and hand it back.");
                    b.Say(g, Lines.T(GirlInfo.ToneOf(g), "Thank you so much! || Thank you! You're so kind. || Ah, thank you so much! || Thank you... that really helped.", "You're a lifesaver! || You saved me! Thanks! || Thanks a million! || Woo! You're the best!", "...Thanks. || ...Thank you. || ...Appreciated. || ...Thanks. Really.", "Oh? A gentleman. Thanks~ || How gallant. Thanks~ || A knight! Thanks~ || Oh, chivalry. Thanks."));
                    b.End();
                    b.Branch("walk"); b.End();
                    return Scenes.PlayOnMap(b, c =>
                    {
                        var seen = Social.GirlsOnMyMap().Where(x => x != g).ToList();
                        if (c != "help") { Social.Add(Social.Kinds.Ignored, g, seen); return; }
                        Social.Add(Social.Kinds.Helped, g, seen);
                        Pay(new Rewards { Favor = 3 }, g, name + " appreciated your help");
                        var likes = GirlInfo.LikesOf(g);
                        if (G.Int >= 40 && likes.Count > 0)
                        {
                            var like = likes[Random.Range(0, likes.Count)];
                            S.Girl(GirlInfo.Key(g)).KnownLikes |= 1 << System.Array.IndexOf(GirlInfo.LikeIds, like);
                            Toasts.Show("You noticed " + name + "'s notebooks are covered in " + GirlInfo.LikeLabel(like) + " doodles. (She likes " + GirlInfo.LikeLabel(like) + ".)");
                        }
                    });
                }

                case "gossip":
                {
                    var girls = Board.Students.OrderBy(_ => Random.value).Take(3).ToList();
                    if (girls.Count == 0) return false;
                    b.Narr("You overhear a group of students gossiping by the window...");
                    foreach (var g in girls)
                    {
                        var m = S.Girl(GirlInfo.Key(g)).Mood;
                        b.Narr("\"Did you see " + GirlInfo.FirstName(g) + " today? She seems " + Lines.MoodName(m).ToLower() + ".\"");
                    }
                    return Scenes.PlayOnMap(b, _ =>
                    {
                        foreach (var g in girls) S.Girl(GirlInfo.Key(g)).MoodKnownDay = S.Day;
                        Toasts.Show("You learned how " + girls.Count + " girls are feeling today.");
                    });
                }

                case "vending":
                {
                    var win = Random.value < 0.25f + G.Sens / 400f;
                    b.Narr("You buy a drink from the vending machine. The little lottery display starts spinning... 7... 7... " + (win ? "7!" : "3."));
                    b.Narr(win ? "A free drink drops out! You feel refreshed." : "Better luck next time.");
                    return Scenes.PlayOnMap(b, _ =>
                    {
                        S.Money = Mathf.Max(0, S.Money - 120);
                        if (win) { S.Energy++; Toasts.Show("Free drink! (+1 energy)"); }
                    });
                }

                case "wind":
                {
                    var g = Near();
                    if (g == null) return false;
                    var name = GirlInfo.FirstName(g);
                    var tone = GirlInfo.ToneOf(g);
                    b.Narr("A sudden gust of wind sweeps the hallway — and " + name + "'s skirt flips up!");
                    var opts = new List<string> { "Look away immediately.", "away", "Pretend you saw nothing.", "nothing" };
                    if (G.Sens >= 30) { opts.Add("\"...Cute.\" [SEN]"); opts.Add("cute"); }
                    b.Choice(opts.ToArray());
                    b.Branch("away");
                    b.Say(g, Lines.T(tone, "Th-thank you for looking away... || Th-thank you... for not looking... || You looked away... thank you... || I-I'm so glad you didn't look...", "Phew! You didn't see anything, right?! RIGHT?! || You didn't see anything, right?! Promise?! || Phew! You saw nothing! Got it?! || Nothing happened! Nothing!", "...You looked away. Good. || ...Good. You looked away. || ...Thank you for your discretion. || ...Decent of you.", "Aww, you didn't even peek? How boring~ || Not even a peek? Disappointing~ || What a gentleman. How dull~ || You didn't look? I'm almost offended."));
                    b.End();
                    b.Branch("nothing");
                    b.Say(g, Lines.T(tone, "Y-you saw, didn't you...? || Y-you saw it... didn't you...? || Ah... you saw... || Please tell me you didn't see...", "You totally saw! || You saw! You totally saw! || Hey! You looked! || Don't lie! You saw!", "...Forget it. Now. || ...Erase that memory. || ...You saw nothing. Understood? || ...Forget it.", "Enjoy the show? || Like the view? || Enjoyed that? || A free show, huh?"));
                    b.End();
                    b.Branch("cute");
                    var lewdOk = g.lewdness >= 40 || tone == Tone.Teasing || g.isGirlfriend;
                    b.Say(g, lewdOk
                        ? Lines.T(tone, "Eh?! ...Y-you think so...? || Eh?! ...R-really...? || W-wait, you think they're cute...? || Ah... um... thank you...?", "Hehe, really? I picked them this morning! || Hehe, right? They're my favorite! || Oh! You like them? Yay! || Really?! Thanks! I picked them myself!", "...Idiot. ...Thanks. || ...Idiot. ...Thank you. || ...Don't comment. ...Thanks. || ...Shut up. ...Thanks.", "Only cute? I'll show you something better later~ || Cute? Wait till you see the rest~ || Just cute? You've seen nothing yet. || Only cute? Hmph. Next time, then.")
                        : Lines.T(tone, "P-pervert!! || P-pervert! || Ah! You're terrible! || N-no! Pervert!", "WHAT?! Pervert! || PERVERT! || Ew! Pervert! || Whaaat?! Creep!", "Die. || Disgusting. || Vanish. || Never speak again.", "Wow. Gross. || Gross. || Ew. Seriously? || Wow. Yikes."));
                    b.End();
                    return Scenes.PlayOnMap(b, c =>
                    {
                        if (c == "away") G.AddFavor(g, tone == Tone.Teasing ? 0 : 2, 0);
                        else if (c == "cute")
                        {
                            if (lewdOk) Pay(new Rewards { Lewd = 5, Sens = 1 }, g, name + " is blushing");
                            else { G.AddFavor(g, -4, 0); Toasts.Show(name + " thinks you're a pervert. (-4 favor)"); }
                        }
                        else G.AddFavor(g, 0, 1);
                    });
                }

                case "rumor":
                {
                    var g = Girl(h => h.favor >= 30 && Social.KnowsYou(h));
                    if (g == null) return false;
                    var name = GirlInfo.FirstName(g);
                    b.Narr("Two classmates whisper as you pass: \"I heard " + name + " has a crush on him...\"");
                    return Scenes.PlayOnMap(b, _ =>
                    {
                        var r = Board.Make(RequestKind.Talk, g, "Is the rumor true?", "People say " + name + " likes you. Go talk to her today.", 6);
                        r.Source = "happening";
                        Board.Add(r);
                        Toasts.Show("New request: talk to " + name + ".");
                    });
                }

                case "inspection":
                {
                    var lewdItems = new[] { "lingerie", "toy", "photo_book" };
                    var carrying = lewdItems.Where(x => S.Count(x) > 0).ToList();
                    b.Narr("Student union inspection! They're checking everyone's bags at the gate.");
                    if (carrying.Count == 0)
                    {
                        b.Narr("Your bag is spotless. The council president nods approvingly.");
                        return Scenes.PlayOnMap(b, _ => Pay(new Rewards { Rep = 2 }, null, "Clean record"));
                    }
                    var opts = new List<string> { "Hand over the bag.", "hand" };
                    if (G.Int >= 40) { opts.Add("\"Those are for the art club's figure study.\" [INT]"); opts.Add("talk"); }
                    if (G.Str >= 40) { opts.Add("Run for it! [STR]"); opts.Add("run"); }
                    b.Choice(opts.ToArray());
                    b.Branch("hand"); b.Narr("The council member pulls out your... items. Everyone stares."); b.End();
                    b.Branch("talk"); b.Narr("You explain with a perfectly straight face..."); b.End();
                    b.Branch("run"); b.Narr("You sprint past the gate before anyone can stop you!"); b.End();
                    return Scenes.PlayOnMap(b, c =>
                    {
                        var ok = c == "talk" ? Campus.Check(G.Int, 70, out roll) : c == "run" && Campus.Check(G.Str, 70, out roll);
                        if (ok) { Toasts.Show("You got away with it."); return; }
                        var item = carrying[Random.Range(0, carrying.Count)];
                        S.AddItem(item, -1);
                        Pay(new Rewards { Rep = -5 }, null, "Confiscated: " + Items.NameOf(item));
                    });
                }

                case "stray_cat":
                {
                    var hasFood = S.Count("cat_food") > 0;
                    b.Narr("A small stray cat follows you, meowing hopefully.");
                    var opts = new List<string> { "Pet it.", "pet" };
                    if (hasFood) { opts.Add("Give it some cat food."); opts.Add("feed"); }
                    b.Choice(opts.ToArray());
                    b.Branch("pet"); b.Narr("It purrs and rubs against your leg, then trots off."); b.End();
                    b.Branch("feed"); b.Narr("It eats happily. A few girls watching from the window go \"awww\"."); b.End();
                    return Scenes.PlayOnMap(b, c =>
                    {
                        if (c != "feed") return;
                        S.AddItem("cat_food", -1);
                        foreach (var h in Board.Students.Where(x => GirlInfo.Likes(x, "animals"))) G.AddFavor(h, 2, 0);
                        Toasts.Show("Animal lovers heard about it. (+2 favor with them)");
                    });
                }

                case "drenched":
                {
                    // A girl runs in from the rain, soaked to the skin (a rain lover laughs about it).
                    var g = Near() ?? Girl();
                    if (g == null) return false;
                    var name = GirlInfo.FirstName(g);
                    var tone = GirlInfo.ToneOf(g);
                    var lover = Weather.RainLover(g);
                    b.Narr(name + " runs in from outside, soaked to the skin, hair stuck to her cheeks.");
                    b.Say(g, lover ? Lines.T(tone, "Ehehe... I couldn't help it. I like the rain... || Ehehe... I like rain. I couldn't resist. || The rain felt nice... sorry. || Hehe... I just wanted to feel it.", "Woo! That was AMAZING! Did you see how hard it's coming down?! || Wooo! The rain's crazy today! Did you see?! || That was so fun! Rain dance! || Yeah! Rain is awesome!", "...I walked. It's just water. || ...It's only water. || ...I didn't mind walking. || ...Rain is fine.", "Don't look at me like that. Rain is fun~ || Rain is fun. You should try it~ || Don't judge. Rain's fun. || What? Wet suits me.")
                                   : Lines.T(tone, "Uuu... it came out of nowhere... I'm all wet... || Uuu... I'm soaked... || Ah... the rain came so suddenly... || I'm all wet... how embarrassing...", "Gah! Totally soaked! My shoes are squishing! || Soaked! Totally soaked! || Ugh, my socks are wet! || I look like a drowned cat!", "...Don't say anything. || ...Not a word. || ...Don't comment. || ...Don't look.", "Like what you see? ...Kidding. I'm freezing. || Enjoying the view? Joke. I'm cold. || Wet look. Trendy, huh? Brr. || Stop staring. ...I'm freezing."));
                    b.Choice("Give her your handkerchief.", "towel", "Give her your jacket.", "jacket", "Laugh it off with her.", "laugh");
                    b.Branch("towel"); b.Say(g, Lines.T(tone, "Thank you... it smells like you. || Thank you... it smells nice. || Thank you... it's warm. || Ah... thank you so much.", "You're a lifesaver! || You're a hero! Thanks! || Thanks! So warm! || Yay! Thank you!", "...Thanks. I'll wash it. || ...Thank you. I'll return it washed. || ...Appreciated. || ...Thanks. Clean, when I return it.", "A gentleman. I'll keep it~ || How sweet. I'm keeping this. || Mine now. Thanks~ || A gift? I accept~")); b.End();
                    b.Branch("jacket"); b.Say(g, Lines.T(tone, "B-but then you'll be cold...! ...It's warm. || B-but you'll be cold... ...It's so warm. || Are you sure...? ...Thank you. || But... ...it's really warm. Thank you.", "Whoa, really?! So warm! || Really?! Whoa, warm! || Your jacket?! Thanks! || Ahh, toasty! You're the best!", "...You'll get sick. Idiot. ...Thank you. || ...You'll catch a cold. ...Thank you. || ...Idiot. ...Thanks. || ...Fine. Thank you.", "Your jacket? Bold move. I'll take it~ || Your jacket? Bold. Mine now~ || Ooh, smooth move. I'll take it. || Giving me your jacket? Adorable.")); b.End();
                    b.Branch("laugh"); b.Say(g, lover ? Lines.T(tone, "Hehe... you get it. || Hehe... you understand. || Ehehe... right? || Mm. You get it.", "Right?! Rain is the best! || Right?! Rain rules! || See?! Best weather! || Yes! Rain buddy!", "...Heh. || ...Heh. || ...Hm. Yes. || ...Right.", "See? Fun~ || Told you. Fun~ || See? Rain's fun. || Admit it. It's fun.") : Lines.T(tone, "I-it's not funny... || D-don't laugh... || I-it's not funny... || Please don't laugh at me...", "Hey! Don't laugh! ...Okay it's a little funny. || Hey! Stop laughing! ...Okay it's funny. || Don't laugh! ...Pfft. || Rude! ...Haha, fine.", "...Hmph. || ...Hmph. || ...Not amusing. || ...Stop.", "Laugh now. I'll drip on you later. || Laugh it up. I'll drip on you. || Oh, you think it's funny? Hug time. || Keep laughing. I'm getting you wet.")); b.End();
                    return Scenes.PlayOnMap(b, c =>
                    {
                        if (c == "towel") Pay(new Rewards { Favor = 4 }, g, "Dried off with your handkerchief");
                        else if (c == "jacket") { Pay(new Rewards { Favor = 6, Lewd = 1 }, g, name + " in your jacket"); Campus.SpendEnergy(1); }
                        else if (lover) Pay(new Rewards { Favor = 3 }, g, "Rain buddies");
                        if (c == "towel" || c == "jacket") Social.Add(Social.Kinds.Helped, g, Witness.Who(g, Witness.Talk));
                    });
                }

                case "rain":
                {
                    var g = Girl();
                    if (g == null) return false;
                    var name = GirlInfo.FirstName(g);
                    b.Narr("It starts pouring right as campus ends. " + name + " is stuck at the entrance without an umbrella.");
                    b.Choice("Share your umbrella with her.", "share", "Lend it to her and run home in the rain.", "lend", "Wait for the rain to stop.", "wait");
                    b.Branch("share");
                    b.Say(g, Lines.T(GirlInfo.ToneOf(g), "Together...? O-okay. Your shoulder is getting wet... || Together...? Okay... you're getting wet, though. || Sharing...? Mm. Your shoulder's wet... || Th-thank you... let's share.", "Umbrella buddies! Squeeze in! || Umbrella squad! Get in here! || Share! Squeeze closer! || Umbrella buddies! Yay!", "...Fine. Don't get any closer than necessary. || ...Fine. Not too close. || ...Acceptable. Keep your distance. || ...Share, then. Carefully.", "Sharing an umbrella? How romantic~ || Sharing an umbrella? So cliché. I love it. || Under one umbrella? How scandalous~ || Romantic, aren't we?"));
                    b.End();
                    b.Branch("lend");
                    b.Say(g, Lines.T(GirlInfo.ToneOf(g), "But you'll get soaked...! Thank you... || But you'll get wet... thank you... || You'll be soaked... thank you so much. || Are you sure...? Thank you...", "Whoa, really?! You're the best! || Seriously?! You rock! || For me?! Best day! || Wow! Thanks!", "...You're an idiot. Thank you. || ...Idiot. Thank you. || ...You'll get sick. Thanks. || ...Foolish. Thank you.", "My knight in soaked armor~ || My soggy hero~ || Drenched chivalry. Cute. || A wet knight. I'm charmed."));
                    b.End();
                    b.Branch("wait"); b.End();
                    return Scenes.PlayOnMap(b, c =>
                    {
                        if (c == "share") Pay(new Rewards { Favor = 5, Lewd = 2 }, g, "Under one umbrella with " + name);
                        else if (c == "lend") { Pay(new Rewards { Favor = 7 }, g, name + " won't forget it"); Campus.SpendEnergy(1); }
                        if (c == "share" || c == "lend") Social.Add(Social.Kinds.Helped, g, Social.GirlsOnMyMap().Where(x => x != g).ToList());
                    });
                }
            }
            return false;
        }
    }
}
