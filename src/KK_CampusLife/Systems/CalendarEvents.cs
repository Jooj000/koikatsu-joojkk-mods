using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Joojkk.CampusLife
{
    /// <summary>
    /// (<see cref="EventBackdrop"/>), three or four choices that meet again (<see cref="SceneBuilder.Goto"/>), and
    /// rewards read from every choice taken (<see cref="Scenes.Picks"/>). Stat checks are rolled before the scene, so
    /// the text can tell how it went.
    /// </summary>
    internal static partial class Calendar
    {
        private static string T(SaveData.Heroine g, string gentle, string energetic, string cool, string teasing)
        {
            return Lines.T(GirlInfo.ToneOf(g), gentle, energetic, cool, teasing);
        }

        private static string N(SaveData.Heroine g) { return GirlInfo.FirstName(g); }

        /// <summary>A stat roll without spending the lucky charm (the charm, if any, is spent once at the end).</summary>
        private static bool Roll(int stat, int difficulty, int charm)
        {
            // Your stat against the bar (2026-11-07: context, not chance); only a tie, within two points, goes to the dice.
            return Consent.Yes(null, "cal:" + difficulty + ":" + stat, 0.5f + (stat + 20 + charm - difficulty) / 50f);
        }

        private static int Charm() { return S.Count("omamori") > 0 ? 15 : 0; }

        private static void UseCharm(int charm)
        {
            if (charm <= 0 || S.Count("omamori") <= 0) return;
            S.AddItem("omamori", -1);
            Toasts.Show("Your lucky charm (omamori) helped.");
        }

        private static SaveData.Heroine Other(SaveData.Heroine not)
        {
            var list = Board.Students.Where(h => h != not).ToList();
            return list.Count == 0 ? null : list[Random.Range(0, list.Count)];
        }

        // ================= Chocolate Day =================

        private static bool Choco()
        {
            var givers = Board.Students.Where(h => h.isGirlfriend || h.favor >= 60).OrderByDescending(h => (h.isGirlfriend ? 200 : 0) + h.favor).Take(6).ToList();
            var top = givers.FirstOrDefault();
            var shy = givers.Count == 0 ? Board.Students.Where(h => h.favor >= 25).OrderByDescending(h => h.favor).FirstOrDefault() ?? AnyGirl() : null;
            var b = new SceneBuilder(null, false);

            b.Narr("February 14th. Chocolate Day.");
            b.Narr("The whole university smells of cocoa. Every boy is pretending not to care, and every boy is failing.");
            b.Narr("Your locker is right in front of you. It might be empty. It might not be.");
            b.Choice("Open it calmly", "ch_calm", "Open it with your eyes closed", "ch_pray", "Walk past it like a cool guy", "ch_cool");
            b.Branch("ch_calm");
            b.Narr(givers.Count > 0 ? "A small box wrapped in red paper slides out. No name on it. Your heart does a little jump." : "Shoes. Just your shoes. Totally expected. Totally fine.");
            b.Goto("ch_class");
            b.Branch("ch_pray");
            b.Narr("You open it slowly, whispering a prayer to every god of romance you know...");
            b.Narr(givers.Count > 0 ? "...and something falls on your foot. A box! With a heart sticker!" : "...and nothing happens. The gods heard you and said no.");
            b.Goto("ch_class");
            b.Branch("ch_cool");
            b.Narr("You walk past without looking. Then you walk back. Then you look.");
            b.Narr(givers.Count > 0 ? "There's a box. You nod, as if you knew all along." : "Empty. Good thing nobody saw you come back.");
            b.Goto("ch_class");

            b.Label("ch_class");
            b.Bg("classroom_sunset");
            b.Narr("Classes drag on forever. The room is full of whispered plans and rustling paper bags.");
            if (givers.Count == 0)
            {
                b.Narr("Lunch comes and goes. Nobody walks up to your desk. You eat your bread very slowly, just in case.");
                if (shy != null)
                {
                    b.Narr("Right before the bell, " + N(shy) + " stops next to you.");
                    b.Say(shy, T(shy, "Um... I made too many for my club. You can have the extra one... if you want. || I had one extra... would you like it? || Um, there was a spare one. It's yours, if you want. || This one was left over... please take it.", "Leftover chocolate! Catch! ...Nice catch! || Spare chocolate! Here! || Extra one! Yours! Catch! || Leftover! Don't complain!",
                        "I had one left over. Don't make it weird. || A spare. Don't read into it. || Extra. Take it. || I had one more. Here.", "Pity chocolate~ Don't cry, it's still chocolate. || Pity chocolate. Be grateful. || Leftovers for you~ Still sweet. || Consolation chocolate. Cheer up."));
                    b.Me("Thanks. Really.");
                }
                b.Narr("Next year, for sure.");
            }
            else
            {
                b.Narr("At lunch, it starts.");
                foreach (var g in givers.Take(3))
                {
                    b.Narr(N(g) + " walks up to your desk, hands hidden behind her back.");
                    b.Say(g, g.isGirlfriend
                        ? T(g, "I made these myself... for you. Only for you. || I made these... just for you. || Homemade... for you alone. || I worked hard on these... they're yours.", "Homemade! Eat them all, okay?! || Homemade! Eat every one! || I made them! All for you! || Kitchen chaos! For you!", "Handmade. Don't make a fuss. || Handmade. No fuss. || I made them. Eat. || Homemade. Don't comment.", "Handmade, with love~ And maybe a little something extra. || Handmade with love. And mischief. || Made by me. Feel special. || Homemade~ You're lucky.")
                        : T(g, "Um, these are... thank-you chocolates! Just thanks! || These are thank-you chocolates! Only thanks! || Just a thank-you! Nothing else! || Um, gratitude chocolate! That's all!", "Here, friendship chocolate! Don't get the wrong idea! || Friend chocolate! No ideas! || Buddy chocolate! Got it?! || Friendship! Only friendship!", "Obligation chocolate. Obviously. || Obligation. Obviously. || Courtesy chocolate. || Duty chocolate.", "Don't get your hopes up. ...Unless you want to. || Don't hope too much. Unless you want to. || Just obligation. Probably. || Courtesy. Or is it?"));
                }
                if (givers.Count > 3) b.Narr("And " + (givers.Count - 3) + " more boxes appear on your desk while you're not looking. The boys at the back are glaring.");
                b.Narr(N(top) + " is still standing there, watching you hold her box.");
                b.Choice("Eat one right now", "ch_eat", "Ask if it's homemade", "ch_home", "Save it for later", "ch_save");
                b.Branch("ch_eat");
                b.Say(top, T(top, "R-right now? ...How is it? Is it okay? || N-now? ...Is it good? || Um... how does it taste? || Do you like it...?", "Well?! Well?! Is it good?! || Well?! Good?! || Tell me! Tasty?! || Verdict?!", "...You have chocolate on your lip. Idiot. || ...Chocolate on your lip. Idiot. || ...You're messy. || ...Wipe your mouth.", "Mmh, you eat it so happily~ I should feed you more often. || You eat so happily. I'll feed you more. || Cute. Want another? || Look at you. Adorable."));
                b.Narr("It's sweet, a little bitter at the end. Perfect.");
                b.Goto("ch_after");
                b.Branch("ch_home");
                b.Say(top, T(top, "Y-yes... I stayed up late. Don't tell anyone. || Yes... I stayed up late. Secret. || I made them last night... shh. || All by myself... it took hours.", "Totally homemade! The kitchen is a disaster now! || Homemade! Kitchen's wrecked! || Totally homemade! Messy! || Made them myself! Chaos!", "Store bought. ...Fine. Homemade. Happy? || Bought. ...Fine. Homemade. || ...Homemade. Satisfied? || I made them. Don't gloat.", "Homemade, with a secret ingredient. Guess what it is~ || Secret ingredient included. Guess. || Homemade. With something special. || Made with a secret. Guess what~"));
                b.Goto("ch_after");
                b.Branch("ch_save");
                b.Say(top, T(top, "Oh... okay. Eat it when you're alone, then. || Okay... eat it alone, then. || Alright... later, then. || Save it? Okay...", "Saving it? Don't let it melt! || Don't let it melt! || Saving it? Eat soon! || Hide it well!", "Hmph. Suit yourself. || Suit yourself. || Hmph. Fine. || As you like.", "Saving the best for last? Smart boy~ || Saving the best for last? Smart. || Later? Good boy. || Savoring me? Clever."));
                b.Goto("ch_after");
                b.Label("ch_after");
            }

            b.Bg("snow_street_night");
            b.Narr("After class. Snow falls softly on the way home, and the streetlights come on one by one.");
            var honmei = top != null && (top.isGirlfriend || top.favor >= 70);
            if (honmei)
            {
                b.Narr("Footsteps run up behind you. " + N(top) + ", out of breath, cheeks red from the cold.");
                b.Say(top, T(top, "Wait! The one this morning was... not the real one. This is the real one. || Wait! That one was... this is the real one. || The real one is this... please take it. || This morning's wasn't true. This one is.", "Hah... hah... I forgot to give you the SPECIAL one! || Forgot the SPECIAL one! Here! || Wait! The real one! || Hah... this one's the real deal!",
                    "...That one on campus was for show. This one is honmei. Don't make me say it twice. || ...This is honmei. Once only. || ...The real one. Don't make me repeat. || ...That was decoy. This is real.", "The one on campus was obligation. This one's... not. Got it? || That was obligation. This isn't. Got it? || The real one. Treasure it. || The campus one was fake. This is real."));
                b.Choice("Promise to return it on White Day", "ch_white", "Take her cold hand", "ch_hand", "Just say thank you", "ch_thanks");
                b.Branch("ch_white");
                b.Me("I'll give you something back on White Day. Something good.");
                b.Say(top, T(top, "...I'll be waiting. Every day until then. || I'll wait... every day. || I'll be looking forward to it. || I'll count the days...", "Three times as good, that's the rule! I'm counting! || Triple return! Counting! || Three times better! Rule! || I'll be waiting! Big!", "I'll hold you to that. || I'll hold you to it. || Expected. || Remembered.", "Something good? Then I'll expect something very good~ || Something very good, then~ || Impress me. || Make it worth it."));
                b.Goto("ch_end");
                b.Branch("ch_hand");
                b.Narr("Her fingers are freezing. She doesn't pull away. For a while, neither of you says anything.");
                b.Say(top, T(top, "...Your hand is warm. || ...Your hand is warm. || ...Warm hands. || ...This is nice.", "W-what's this all of a sudden?! ...Don't let go. || Wh-what?! ...Don't let go! || Whoa! ...Keep holding! || Huh?! ...Okay! Stay!", "...Only because it's cold. || ...Only because it's cold. || ...Cold weather. That's all. || ...For warmth.", "Took you long enough~ || Took you long enough~ || Finally~ || About time."));
                b.Goto("ch_end");
                b.Branch("ch_thanks");
                b.Me("Thank you. I mean it.");
                b.Say(top, T(top, "Mm. That's all I wanted to hear. || That's all I wanted. || Mm. That's enough. || I'm happy.", "Hehe! You're welcome! || You're welcome! || Hehe! Anytime! || No problem!", "...You're welcome. || ...Welcome. || ...Mm. || ...Fine.", "Just 'thank you'? Well, it's a start~ || Just thanks? A start. || Only thanks? Hm. || Thanks? More later."));
                b.Goto("ch_end");
                b.Label("ch_end");
            }
            else b.Narr("You walk home alone, counting the boxes in your bag. Not a bad day at all.");
            b.Narr("Chocolate Day is over. Your bag is heavier, and somehow so is your heart.");

            return Scenes.PlayOnMap(b, _ =>
            {
                var p = Scenes.Picks;
                foreach (var g in givers) { S.AddItem("chocolate", 1); G.AddFavor(g, 2, 0); }
                if (givers.Count > 0) Pay(new Rewards { Rep = Math.Min(5, givers.Count) }, null, "You received " + givers.Count + " chocolate(s)");
                else if (shy != null) { S.AddItem("chocolate", 1); Pay(new Rewards { Favor = 3 }, shy, "A leftover chocolate from " + N(shy)); }
                if (top != null)
                {
                    if (p.Contains("ch_eat")) G.AddFavor(top, 4, 1);
                    else if (p.Contains("ch_home")) G.AddFavor(top, 3, 0);
                    else if (p.Contains("ch_save")) G.AddFavor(top, 1, 0);
                    if (p.Contains("ch_white")) Pay(new Rewards { Favor = 6, Devotion = 2 }, top, "You promised " + N(top) + " a White Day gift");
                    else if (p.Contains("ch_hand")) Pay(new Rewards { Favor = 5, Lewd = 3 }, top, "Walking home hand in hand with " + N(top));
                    else if (p.Contains("ch_thanks")) Pay(new Rewards { Favor = 4 }, top, N(top) + "'s real chocolate");
                }
            });
        }

        // ================= Sports Festival =================

        private static bool Sports()
        {
            var partner = TopGirls(1).FirstOrDefault();
            var rep = Other(partner);
            var charm = Charm();
            var relayWin = Roll(G.Str, 65, charm);
            var tugWin = Roll(G.Str / 2 + 25, 55, charm);
            var cheerHit = Roll(G.Sens, 60, charm);
            var threeWin = partner != null && Roll((G.Str + partner.favor) / 2, 70, charm);
            var teamWin = Consent.Yes(null, "cal:team", 0.3f + 0.15f * ((relayWin ? 1 : 0) + (tugWin ? 1 : 0) + (cheerHit ? 1 : 0) + (threeWin ? 1 : 0)) + S.Reputation / 500f);
            var b = new SceneBuilder(null, false);

            b.Bg("sports_tents");
            b.Narr("The Sports Festival! Flags snap in the wind and the whole university is out on the field.");
            b.Narr("Tents, tables, a mountain of traffic cones and a professor with a megaphone who is already hoarse.");
            if (rep != null)
                b.Say(rep, T(rep, "Here's your class headband... Tie it tight, okay? Good luck today. || Your headband... tie it tight. Good luck. || Here, for you. Do your best today. || Our class headband... I'll be cheering.", "HEADBANDS! Everybody gets one! Our class is winning this! || HEADBANDS! We're winning! || Put it on! Victory! || Class colors! Let's go!",
                    "Headband. Put it on properly. We're not losing to the other classes. || Wear it properly. We won't lose. || Headband. Tight. || Properly. We win today.", "Headband~ Let me tie it for you. ...There. Now you look like you mean it. || Let me tie it. ...There. Serious face. || I'll tie it. Look heroic. || Headband on. Now you look the part."));
            if (partner != null)
                b.Say(partner, T(partner, "Which event are you entering? I'll cheer for you from the tent! || Which event? I'll cheer from the tent! || What are you entering? I'll watch! || I'll cheer for you, okay?", "Let's WIN this! Which event are you in? || Let's WIN! Your event?! || Which race?! Go go! || What event?! Crush it!",
                    "Try not to embarrass our class. || Don't embarrass us. || Represent us well. || Try not to lose.", "I'll be watching you~ Don't trip. || Watching you. Don't trip. || I'll be watching~ || Eyes on you. Perform."));
            b.Narr("The sign-up sheet is in front of you. Pick your event.");
            var opts = new List<string> { "Relay race [STR]", "sp_relay", "Tug of war [STR  team]", "sp_tug", "Cheer squad [SEN]", "sp_cheer" };
            if (partner != null) { opts.Add("Three-legged race with " + N(partner)); opts.Add("sp_three"); }
            b.Choice(opts.ToArray());

            b.Branch("sp_relay");
            b.Bg("sports_track");
            b.Narr("Anchor leg. The baton is coming toward you, and your class is in third place.");
            b.Narr("You grab it and run like your life depends on it. The crowd becomes one long roar.");
            b.Narr(relayWin ? "You pass the second runner on the curve, then the first one at the line. Your class goes wild!" : "You catch up to second place... almost. Your legs give out ten meters too early.");
            b.Goto("sp_girls");
            b.Branch("sp_tug");
            b.Bg("sports_track");
            b.Narr("Thirty students on one rope. The referee raises the flag.");
            b.Narr("PULL! Your feet slide in the dirt. Somebody behind you is screaming a war cry.");
            b.Narr(tugWin ? "Inch by inch, the red ribbon crosses the line on your side. Victory!" : "The rope slips. The whole team falls over like dominoes, laughing anyway.");
            b.Goto("sp_girls");
            b.Branch("sp_cheer");
            b.Bg("sports_track");
            b.Narr("You grab the megaphone and lead the cheering section with all your heart.");
            b.Narr(cheerHit ? "By the third song even the professors are clapping along. Your voice carries across the whole field." : "Your voice cracks on the big note. Half the stand laughs. The other half cheers louder for your courage.");
            b.Goto("sp_girls");
            if (partner != null)
            {
                b.Branch("sp_three");
                b.Bg("sports_track");
                b.Narr("Your ankle is tied to hers. You'll have to move together.");
                b.Say(partner, T(partner, "W-we're really close like this... Left foot first, okay? || So close... left foot first. || We're touching... um, left foot. || Ready? Left first...", "Okay! One-two, one-two! Don't let go of me! || One-two! Hold tight! || Go go! Don't let go! || Left, right! Teamwork!",
                    "Keep up. And don't hold me that tight. || Keep pace. Loosen your grip. || Not so tight. Keep up. || Pace. Now.", "Arm around my waist~ For balance, of course. || Arm on my waist. For balance~ || Hold me closer. For the race. || Closer. Strategy."));
                b.Narr("The starting pistol fires. Her shoulder bumps yours with every step.");
                b.Narr(threeWin ? "Somehow you find the rhythm. You cross the line first, still tied together, both laughing." : "Halfway there you both go left at the same time and tumble into the grass, tangled up and laughing.");
                b.Goto("sp_girls");
            }

            b.Label("sp_girls");
            b.Narr("Afternoon. The girls' 200 meters is next.");
            if (partner != null)
            {
                b.Narr(N(partner) + " is in lane four, bouncing on her toes, glancing toward your class's tent.");
                b.Choice("Shout her name as loud as you can", "sp_shout", "Wait at the finish line with water", "sp_water", "Film it for her", "sp_film");
                b.Branch("sp_shout");
                b.Narr("\"GO, " + N(partner).ToUpperInvariant() + "!\" Half the field turns to look at you. She hears it. You see her smile as she runs.");
                b.Say(partner, T(partner, "I heard you... It was so embarrassing. ...Thank you. || I heard you... so embarrassing... thank you. || Your voice... I ran harder. Thank you. || You cheered for me... thank you.", "THAT WAS YOU?! I ran faster because of that! || THAT WAS YOU?! I ran faster! || You yelled! It worked! || Your cheering! Boost!", "You're loud. ...I didn't hate it. || Loud. ...Not bad. || ...Noisy. Thanks. || ...You're loud. I liked it.", "Shouting my name in front of everyone? Bold~ || Yelling my name? Bold. || Cheering so loud? Cute. || Everyone heard. Brave."));
                b.Goto("sp_close");
                b.Branch("sp_water");
                b.Narr("She crosses the line and nearly collapses. You're right there with a cold bottle.");
                b.Say(partner, T(partner, "You waited for me...? Thank you, I'm so thirsty. || You waited? Thank you. So thirsty... || Water... thank you so much. || You're so thoughtful...", "Water! You're a lifesaver! || Water! Lifesaver! || Yes! Hydration! || Thank you! Drink!", "...Thoughtful. Thanks. || ...Thoughtful. || ...Thanks. || ...Considerate.", "Waiting at the finish line for me? How devoted~ || Waiting for me? Devoted. || At the finish line? Sweet. || Loyal boy."));
                b.Goto("sp_close");
                b.Branch("sp_film");
                b.Narr("You record the whole race on your phone. Later she watches it three times, pretending she isn't.");
                b.Say(partner, T(partner, "Don't show anyone how my face looks when I run! || Don't show my running face! || Please delete it... I look silly. || Don't let anyone see!", "Again! Play it again! Look how fast I was! || Again! Look how fast! || Replay! Replay! || Look at me go!", "Delete the part where I almost tripped. || Delete the trip. || Cut the stumble. || Edit that out.", "Keeping a video of me? What else do you keep~? || Keeping videos of me? What else? || Saving me? Interesting. || A collection? Creepy. Cute."));
                b.Goto("sp_close");
                b.Label("sp_close");
            }
            else b.Narr("You cheer from the tent until your throat hurts.");

            b.Bg("gym");
            b.Narr("Closing ceremony in the gym. The scoreboard is covered with a cloth while the dean talks... and talks.");
            b.Narr(teamWin ? "The cloth comes off. YOUR TEAM WON! The whole class jumps at once and the floor shakes." : "The cloth comes off. Second place. Close. Next year, the trophy is yours.");
            if (partner != null)
            {
                b.Say(partner, teamWin ? T(partner, "We won... we really won! I'm so glad I was on your team. || We won! I'm glad we were a team. || We really won... thank you! || Victory... with you.", "WE WON WE WON WE WON! || WE WON! || Champions! || Yes yes YES!", "Of course we won. ...Okay, I'm happy. A little. || Of course. ...I'm happy. Slightly. || Won. Pleased. || Victory. Good.", "Winners deserve a reward, don't they~? || Winners get rewards~ || Prize time? || Reward me.")
                                       : T(partner, "We did our best. That's what matters. || We tried our best. || It's okay. We did well. || Our best was enough.", "Argh! Next year we crush them! || Next year, revenge! || Argh! Next time! || We'll crush them next year!", "Second place. Acceptable. Barely. || Second. Barely acceptable. || Acceptable. || Close. Not enough.", "Losing is fine. You looked cool anyway~ || You looked cool anyway. || Losing's fine. You were cute. || Still cool, loser~"));
                b.Choice("Walk her home", "sp_home", "Stay and help clean up", "sp_clean");
                b.Branch("sp_home");
                b.Narr("The two of you walk home in the orange evening, sore and sunburned and very happy.");
                b.Goto("sp_end");
                b.Branch("sp_clean");
                b.Narr("You fold tents and carry cones until dark. The professors notice. So does she: she stays to help.");
                b.Goto("sp_end");
                b.Label("sp_end");
            }
            b.Narr("The Sports Festival is over. Tomorrow, everything will hurt.");

            return Scenes.PlayOnMap(b, _ =>
            {
                var p = Scenes.Picks;
                UseCharm(charm);
                if (p.Contains("sp_tug")) Pay(tugWin ? new Rewards { Rep = 4, Str = 1, Club = 2 } : new Rewards { Rep = 1, Str = 1 }, null, tugWin ? "Your team won the tug of war!" : "Your team lost the tug of war");
                else if (p.Contains("sp_cheer"))
                {
                    Pay(cheerHit ? new Rewards { Rep = 5, Sens = 1 } : new Rewards { Rep = 1 }, null, cheerHit ? "Your cheering became the talk of the university!" : "Your cheering was... enthusiastic");
                    foreach (var g in TopGirls(3)) G.AddFavor(g, 2, 0);
                }
                else if (p.Contains("sp_three") && partner != null)
                    Pay(threeWin ? new Rewards { Favor = 10, Lewd = 3, Rep = 3 } : new Rewards { Favor = 6, Lewd = 2 }, partner,
                        threeWin ? "You and " + N(partner) + " won the three-legged race!" : "You and " + N(partner) + " fell over laughing");
                else Pay(relayWin ? new Rewards { Rep = 6, Str = 2, Club = 2 } : new Rewards { Rep = 1, Str = 1 }, null, relayWin ? "You won the relay race!" : "You finished the relay in the middle of the pack");
                if (partner != null)
                {
                    if (p.Contains("sp_shout")) G.AddFavor(partner, 4, 0);
                    else if (p.Contains("sp_water")) G.AddFavor(partner, 5, 0);
                    else if (p.Contains("sp_film")) G.AddFavor(partner, 3, 1);
                    if (p.Contains("sp_home")) Pay(new Rewards { Favor = 5, Lewd = 1 }, partner, "Walking " + N(partner) + " home after the festival");
                    else if (p.Contains("sp_clean")) Pay(new Rewards { Favor = 3, Rep = 2 }, partner, "You and " + N(partner) + " stayed to clean up");
                }
                if (teamWin) Pay(new Rewards { Rep = 3, Club = 1 }, null, "Your team won the Sports Festival");
                foreach (var h in Board.Students.Where(x => GirlInfo.InSportsClub(x) || GirlInfo.Likes(x, "exercise")))
                    if (G.Str >= 50) G.AddFavor(h, 2, 0); // sporty girls noticed you
            });
        }

        // ================= Exams =================

        private static bool Exam()
        {
            var girl = AnyGirl();
            var charm = Charm();
            var peekOk = Roll(G.Sens, 85, charm);
            var hardOk = Roll(G.Int, 60, charm);
            var quizOk = Roll(G.Int, 45, 0);
            var b = new SceneBuilder(null, false);

            b.Bg("classroom_exam");
            b.Narr("Exam day. Desks pulled apart, the schedule written on the board, and a silence you could cut with a ruler.");
            b.Narr("Ten minutes before the start. Everyone is flipping through notes as if the pages might change.");
            if (girl != null)
            {
                b.Narr(N(girl) + " leans over from the next desk, notebook clutched to her chest.");
                b.Say(girl, T(girl, "Um... could you quiz me? Just the formulas... I'm so nervous. || Could you quiz me? I'm nervous. || Please test me on formulas... || Help me review?", "Help! I forgot EVERYTHING! Quick, ask me something! || Forgot everything! Quiz me! || Ask me stuff! Quick! || Brain empty! Help!",
                    "Ask me three questions. I want to be sure. || Three questions. || Test me. || Quiz me. Briefly.", "Quiz me~ If I get them all right, you owe me something. || Quiz me. Perfect score earns a prize. || Test me. Wrong answers, you pay. || Quiz time. Stakes high."));
                b.Choice("Quiz her on the formulas [INT]", "ex_quiz", "Calm her down", "ex_calm_her", "Review on your own", "ex_alone");
                b.Branch("ex_quiz");
                b.Narr(quizOk ? "You fire questions and she answers every one. By the last one she's smiling." : "You mix up two formulas yourself. She corrects you. Now you're the nervous one.");
                b.Goto("ex_start");
                b.Branch("ex_calm_her");
                b.Me("You studied. You know this. Breathe.");
                b.Say(girl, T(girl, "...Okay. Breathe. Thank you. I feel better. || Breathe... thank you. || Okay... I'm calmer. Thanks. || I feel better now.", "Breathe! Right! Hooo... okay! I'm ready! || Breathe! Okay! Ready! || Hooo! Okay! || Calm! Sort of! Go!", "...Fine. I'm calm. I was always calm. || ...Calm. Always was. || ...Fine. || ...Composed.", "You're cute when you try to comfort me~ || Cute, comforting me. || You're sweet when you worry. || Comforting me? Adorable."));
                b.Goto("ex_start");
                b.Branch("ex_alone");
                b.Narr("You bury your face in your notes. One last look at everything.");
                b.Goto("ex_start");
            }
            b.Label("ex_start");
            b.Narr("The professor hands out the papers face down. \"Begin.\"");
            var opts = new List<string> { "Stay calm and start from the top", "ex_calm", "Write the formulas in the margin first", "ex_cram" };
            if (G.Sens >= 60) { opts.Add("Peek at the answer key on the professor's desk [SEN]"); opts.Add("ex_peek"); }
            b.Choice(opts.ToArray());
            b.Branch("ex_calm");
            b.Narr("Deep breath. Question one. You know this. Question two. You know this too.");
            b.Goto("ex_mid");
            b.Branch("ex_cram");
            b.Narr("You scribble every formula you crammed into the margin before they leak out of your head.");
            b.Goto("ex_mid");
            b.Branch("ex_peek");
            b.Narr("The professor steps out to answer the phone. The answer key is right there, on the desk...");
            b.Narr(peekOk ? "You lean back, glance, memorize three answers. Nobody saw. Your heart is pounding." : "\"What are you doing?\" The professor is back. The whole class turns around. Your paper is taken away.");
            b.Goto("ex_mid");

            b.Label("ex_mid");
            b.Narr("Question seven. Nothing you studied looks anything like question seven.");
            b.Choice("Skip it and come back later", "ex_skip", "Fight it to the end [INT]", "ex_fight");
            b.Branch("ex_skip");
            b.Narr("You leave it and finish the rest. At the end you come back with five minutes left and write something reasonable.");
            b.Goto("ex_end");
            b.Branch("ex_fight");
            b.Narr(hardOk ? "Twenty minutes later it clicks. The answer is beautiful. You almost laugh out loud." : "Twenty minutes later you're still on question seven. You rush through the last page.");
            b.Goto("ex_end");

            b.Label("ex_end");
            b.Bg("classroom_sunset");
            b.Narr("\"Pencils down.\" The room exhales all at once.");
            if (girl != null)
                b.Say(girl, T(girl, "How did it go? I... think I did okay. Thanks to you, maybe. || How was it? I think I did okay. || I think it went well... thanks to you. || Did you do okay? I hope so.", "DONE! Wanna compare answers? No? Okay, food then! || DONE! Food time! || Finished! Celebrate! || Over! Let's eat!",
                    "Don't ask me about question seven. || Not question seven. || Don't mention seven. || Skip question seven.", "You looked so serious~ Was it hard, or were you thinking about me? || So serious. Thinking of me? || Hard test, or hard thoughts? || Were you thinking of me during it?"));
            b.Narr("The sun sets over the empty desks. Results tomorrow.");

            return Scenes.PlayOnMap(b, _ =>
            {
                var p = Scenes.Picks;
                UseCharm(charm);
                var score = ExamScore(p.Contains("ex_cram"));
                if (p.Contains("ex_cram")) Campus.SpendEnergy(1);
                if (p.Contains("ex_quiz")) score += quizOk ? 3 : -1;
                if (p.Contains("ex_fight")) score += hardOk ? 6 : -4;
                else if (p.Contains("ex_skip")) score += 2;
                if (p.Contains("ex_peek"))
                {
                    if (peekOk) { score += 20; Toasts.Show("Nobody noticed. Smooth."); }
                    else
                    {
                        score -= 25;
                        S.Reputation = Mathf.Clamp(S.Reputation - 10, 0, 100);
                        Toasts.Show("Caught! The professor confiscates your test. (-10 reputation)");
                    }
                }
                RecordExam(Mathf.Clamp(score, 0, 100));
                if (girl != null)
                {
                    if (p.Contains("ex_quiz")) Pay(new Rewards { Favor = quizOk ? 5 : 2, Int = quizOk ? 1 : 0 }, girl, "You quizzed " + N(girl) + " before the exam");
                    else if (p.Contains("ex_calm_her")) Pay(new Rewards { Favor = 4 }, girl, "You calmed " + N(girl) + " down before the exam");
                }
            });
        }

        // ================= Culture Festival =================

        private static bool Culture()
        {
            var tops = TopGirls(3);
            var helper = tops.FirstOrDefault() ?? AnyGirl();
            var charm = Charm();
            var hit = new Dictionary<string, bool> { { "maid", Roll(G.Sens, 65, charm) }, { "haunt", Roll(G.Str, 65, charm) }, { "science", Roll(G.Int, 65, charm) } };
            var fortune = Random.Range(0, 3);
            var b = new SceneBuilder(null, false);

            b.Bg("classroom_sunset");
            b.Narr("Two weeks before the Culture Festival. The department meeting has been going in circles for an hour.");
            b.Narr("Three ideas are left on the board. The course rep looks at you: you get the deciding vote.");
            b.Choice("Maid cafe [SEN]", "cf_maid", "Haunted house [STR]", "cf_haunt", "Science exhibit [INT]", "cf_science");
            foreach (var booth in new[] { "maid", "haunt", "science" })
            {
                b.Branch("cf_" + booth);
                b.Narr(booth == "maid" ? "Maid cafe it is. Half the class cheers, the other half is already arguing about the outfits."
                    : booth == "haunt" ? "Haunted house! Somebody volunteers a skeleton from the science room. Nobody asks how."
                    : "Science exhibit. The class groans, then gets weirdly excited about a volcano.");
                if (helper != null)
                {
                    b.Narr("You stay late to work on it with " + N(helper) + ".");
                    b.Say(helper, booth == "maid"
                        ? T(helper, "The aprons are done... Do you think this one suits me? || The aprons are done... does it suit me? || Do I look okay in this? || I sewed this... how is it?", "Look, look! I'm practicing my 'Welcome home, master'! || 'Welcome home, master!' Practicing! || Look! Café mode! || Maid practice! How's this?!", "I'm in charge of the menu. Nobody touches the menu. || I control the menu. Don't touch. || Menu is mine. || Hands off the menu.", "Want a private preview of the uniform~? || Private preview? || Want to see the outfit first? || Sneak peek?")
                        : booth == "haunt"
                        ? T(helper, "I'm painting the blood... it's just red paint, right? Right? || It's only paint, right? Right? || Fake blood... I hope... || It's just red paint...", "BOO! Hahaha, your face! || BOO! Haha! || Gotcha! Ahaha! || RAWR! Your face!", "The lighting is wrong. Move that lamp. No, the other one. || Lighting's wrong. Move it. || That lamp. Left. || Fix the light.", "I'll play the ghost who drags boys into the dark~ || I'll drag boys into the dark. || I'm the scary ghost. Beware. || Fear me, mortal.")
                        : T(helper, "The volcano works! I'm so happy I could cry. || It works! I could cry! || The volcano erupted... I'm so happy! || It worked! Yay...", "Can we make it BIGGER? Like, way bigger? || BIGGER! Make it bigger! || More! Huge! || Explosion size up!", "The numbers check out. It'll work. || Numbers check. It works. || Calculated. Success. || As predicted.", "Explaining chemistry to strangers? I'll do it in a lab coat~ || Lab coat chemistry. Sexy science. || Chemistry with me? Explosive. || I'll explain in a lab coat. Watch."));
                }
                b.Bg("culture_festival");
                b.Narr("Festival day! Lanterns over every gate, music from the gym, and the smell of takoyaki everywhere.");
                b.Narr("Visitors pour in. Your class's booth opens its doors, and all morning you work without a break.");
                if (booth == "maid") b.Narr(hit[booth] ? "\"Welcome home!\" By noon the line goes around the corner, and you've learned to carry four parfaits at once." : "The cafe is cozy and half empty. The cake is good, at least. You eat a lot of it.");
                else if (booth == "haunt") b.Narr(hit[booth] ? "The screams can be heard from the gate. A group of third-years comes out pale and immediately gets back in line." : "A little kid laughs at your monster costume. Then another. It's that kind of haunted house.");
                else b.Narr(hit[booth] ? "The volcano erupts on schedule every half hour, and a crowd waits for each one. A professor asks for your notes." : "The volcano fizzles. You explain why, very scientifically, to a very bored grandpa.");
                b.Goto("cf_free");
            }

            b.Label("cf_free");
            b.Narr("In the afternoon you finally get some free time.");
            var opts = new List<string>();
            for (var i = 0; i < tops.Count; i++) { opts.Add("Walk around with " + N(tops[i])); opts.Add("cf_g" + i); }
            opts.Add("Help backstage"); opts.Add("cf_help");
            b.Choice(opts.ToArray());

            for (var i = 0; i < tops.Count; i++)
            {
                var g = tops[i];
                var k = "cf_g" + i;
                b.Branch(k);
                b.Say(g, T(g, "You want to walk with me? ...I'd love to. || Walk with me? I'd love to. || You want to come with me? Yes... || Together? Okay!", "Yes! Let's hit every food stand! || Every stand! Let's go! || Food tour! Yes! || Festival run!", "...Fine. You can escort me. || ...Escort me. || ...Fine. Come. || ...You may accompany me.", "A festival date? Bold~ I accept. || Festival date? Accepted. || A date? Okay~ || Bold. Yes."));
                b.Narr("Where to first?");
                b.Choice("Share some takoyaki", k + "_food", "Brave the haunted house together", k + "_scare", "Visit the fortune teller", k + "_fortune");
                b.Branch(k + "_food");
                b.Narr("The takoyaki stand is run by the cooking club. The octopus pieces are suspiciously big.");
                b.Say(g, T(g, "Hot, hot... Ah, you have sauce right here. Hold still. || Hot! ...Sauce on your cheek. Hold still. || Careful, hot... let me wipe that. || Mm... you have sauce here.", "Mmh! Okay, the next stand is on me! || Next stand's on me! || Yum! More! || Delicious! Next!", "...It's good. Don't watch me eat. || ...Good. Don't watch me eat. || ...Tasty. Eyes elsewhere. || ...Fine. Stop staring.", "Say 'ah'~ Come on, nobody's looking. || Say 'ah'~ || Open wide~ || Feed time~"));
                b.Me("This is the best takoyaki I've ever had.");
                b.Say(g, T(g, "Maybe because you're eating it with me...? || Because we're together...? || It tastes better with you... || Maybe it's the company.", "Right?! Let's get another box! || Another box! || Let's get more! || Seconds!", "It's average. ...The company isn't bad. || Average. Company's fine. || Ordinary. You aren't. || Normal food. Good company.", "Of course. I made it taste better just by being here~ || I made it better. Obviously. || My presence. Flavor boost. || I'm the seasoning."));
                b.Goto(k + "_close");
                b.Branch(k + "_scare");
                b.Bg("haunted_house");
                b.Narr("The third-years built the haunted house. Paper lanterns, cobwebs, and a corridor that goes on far too long.");
                b.Narr("It's pitch dark. Something wet touches your neck. A scream. She grabs your arm and doesn't let go.");
                b.Say(g, T(g, "I-I wasn't scared! ...Don't let go yet, okay? || I wasn't scared! ...Hold on. || N-not scared...! Don't let go... || I'm fine...! Stay close...", "AAAH! Ha... haha! Again! Let's go again! || AAAH! Again! || Haha! One more! || Again! Again!", "I'm not holding on to you. You're holding on to me. || You're holding me. Not reverse. || I'm not clinging. You are. || ...You grabbed me.", "You screamed louder than me~ Cute. || You screamed louder. Cute. || Scared? Adorable. || Who screamed? You."));
                b.Narr("You come out the other side still arm in arm. Neither of you mentions it.");
                b.Goto(k + "_close");
                b.Branch(k + "_fortune");
                b.Bg("fortune_teller");
                b.Narr("A tent of purple curtains, a crystal ball, and a third-year in a veil who takes this very seriously.");
                b.Narr("She looks at the two of you, then at your joined shadows, and smiles.");
                b.Narr(fortune == 0 ? "\"A red thread ties you two. Thin now. It will grow strong.\"" : fortune == 1 ? "\"Storms first, then clear skies. Hold on to each other.\"" : "\"I see a confession... under fireworks... very soon.\"");
                b.Say(g, T(g, "She said... we're compatible. W-what do you think? || Compatible... what do you think? || She said we match... || Um... do you believe it?", "Did you hear that?! Ha, she says that to everyone. ...Right? || She says that to everyone! Right?! || Ha! Fortune! ...Right?! || Totally random! ...Maybe!",
                    "Fortune telling is nonsense. ...What did she say about us, exactly? || Nonsense. ...What exactly did she say? || Rubbish. ...Repeat it. || Fake. ...Details?", "Well? Going to argue with fate? || Arguing with fate? || Destiny says yes. You? || Fate agrees with me."));
                b.Goto(k + "_close");

                b.Label(k + "_close");
                b.Bg("festival_stalls_night");
                b.Narr("Evening. The stalls glow red and orange. In the campus grounds, the bonfire for the closing dance is lit.");
                b.Say(g, T(g, "The folk dance is starting... um... || The dance is starting... um... || Would you... dance? || The folk dance... um...", "Bonfire! Dance! Come on, come on! || Dance! Come on! || Bonfire dance! Go! || Let's dance!", "Are you going to dance? ...Just asking. || Dancing? ...Asking. || ...Will you dance? || ...Are you going?", "Everyone's pairing up. Don't keep a girl waiting~ || Don't keep me waiting. || Ask me already. || Everyone's paired. Hurry."));
                b.Choice("Ask her to dance", k + "_dance", "Watch the fire together", k + "_watch");
                b.Branch(k + "_dance");
                b.Narr("You hold out your hand. She takes it. You step on her foot twice. She doesn't care.");
                b.Say(g, T(g, "I'll remember this festival forever. || I'll never forget tonight. || This festival... forever. || I'll remember this always.", "Again! One more song! || One more song! || Again! || More dancing!", "...You're a terrible dancer. Don't stop. || Terrible dancer. Continue. || Clumsy. Don't stop. || ...Keep going.", "Your hand's sweaty~ Nervous? || Sweaty hand. Nervous? || Nervous? Cute. || Shaky. Adorable."));
                b.Goto("cf_end");
                b.Branch(k + "_watch");
                b.Narr("You sit on the steps and watch the sparks rise into the dark. Her shoulder leans against yours.");
                b.Goto("cf_end");
            }
            b.Branch("cf_help");
            b.Narr("You spend the afternoon backstage: fixing lights, carrying boxes, saving a play from a broken curtain.");
            b.Narr("By evening, the professors know your name. In a good way, for once.");
            b.Goto("cf_end");

            b.Label("cf_end");
            b.Narr("The last song ends. The Culture Festival is over, and nobody wants to go home.");

            return Scenes.PlayOnMap(b, _ =>
            {
                var p = Scenes.Picks;
                UseCharm(charm);
                var booth = p.Contains("cf_haunt") ? "haunt" : p.Contains("cf_science") ? "science" : "maid";
                if (hit[booth]) Pay(new Rewards { Money = 2500, Rep = 5, Club = 3 }, null, "Your class booth was a hit");
                else Pay(new Rewards { Money = 800, Rep = 2, Club = 1 }, null, "Your class booth did okay");
                if (helper != null) G.AddFavor(helper, 3, 0);
                if (p.Contains("cf_help")) { Pay(new Rewards { Rep = 3, Int = 1 }, null, "You helped backstage"); return; }
                for (var i = 0; i < tops.Count; i++)
                {
                    var k = "cf_g" + i;
                    if (!p.Contains(k)) continue;
                    var g = tops[i];
                    if (p.Contains(k + "_food")) Pay(new Rewards { Favor = 8, Lewd = 2, Money = -500 }, g, "Takoyaki date with " + N(g));
                    else if (p.Contains(k + "_scare")) Pay(new Rewards { Favor = 7, Lewd = 5 }, g, "Haunted house with " + N(g) + ", arm in arm");
                    else Pay(new Rewards { Favor = 10, Lewd = 2 }, g, "The fortune teller read you and " + N(g));
                    if (p.Contains(k + "_dance")) Pay(new Rewards { Favor = 6, Lewd = 2, Devotion = 2 }, g, "The bonfire dance with " + N(g));
                    else Pay(new Rewards { Favor = 4, Lewd = 1 }, g, "Watching the bonfire with " + N(g));
                }
            });
        }

        // ================= Tanabata =================

        private static bool Tanabata()
        {
            var g = TopGirls(1).FirstOrDefault();
            var opens = g != null && (g.isGirlfriend || g.favor >= 60);
            var b = new SceneBuilder(null, false);

            b.Bg("tanabata_street");
            b.Narr("July 7th. Tanabata.");
            b.Narr("Once a year, the weaver princess Orihime and the cowherd Hikoboshi cross the river of stars to meet.");
            b.Narr("On earth, people write their wishes on strips of paper and tie them to bamboo, hoping the stars will read them.");
            b.Narr("The old street near campus is full of bamboo branches heavy with colored paper. Lanterns sway overhead.");
            if (g != null)
            {
                b.Narr("Someone tugs your sleeve. " + N(g) + ", in a summer yukata.");
                b.Say(g, T(g, "Ah... you came too. Does it... look okay on me? || You came... does it look okay? || Ah... how do I look? || Is it nice on me?", "Ta-da! Yukata! Say it's cute! || Ta-da! Cute, right?! || Yukata! Say it's cute! || Look! Pretty?!", "My mother made me wear it. Don't stare. || Mother insisted. Don't stare. || ...Don't stare. || ...Obligatory. Stop looking.", "Like what you see~? It took an hour to tie. || Like it? An hour to tie. || Admiring? Good. || Stare all you want."));
                b.Narr("She has a paper strip in her hand and hides it the moment you look.");
                b.Say(g, T(g, "Ah! Don't look at mine... It's embarrassing. || Don't look at mine... || Please don't read it... || It's embarrassing...", "I wished for something HUGE. Not telling! || HUGE wish! Secret! || Big wish! Not telling! || Secret wish!", "Mine is private. Turn around. || Private. Turn around. || Don't look. || Mine. Private.", "Want to know what I wished for? It involves you~ || My wish involves you~ || Guess my wish. Hint: you. || About you. Curious?"));
                b.Narr("She ties it up high, out of your reach.");
            }
            b.Narr("Your turn. You hold the pen over the paper for a long moment...");
            b.Choice("'I want to get smarter.'", "tb_int", "'I want to get stronger.'", "tb_str", "'I want to be more charming.'", "tb_sen", "'I want to be loved.'", "tb_love");
            foreach (var w in new[] { "int", "str", "sen", "love" })
            {
                b.Branch("tb_" + w);
                b.Narr("You hang your wish on the highest branch you can reach. The paper turns slowly in the warm wind.");
                b.Goto("tb_stalls");
            }

            b.Label("tb_stalls");
            b.Bg("festival_stalls_night");
            b.Narr("Night falls. The stalls along the shrine road light up one after another.");
            if (g != null)
            {
                b.Say(g, T(g, "Will you walk with me a little...? || Walk with me...? || Would you stroll with me? || Let's walk a little.", "Stalls! Let's try everything! || Stalls! All of them! || Food! Games! Go! || Let's try everything!", "I'm hungry. You're paying. || Hungry. You pay. || Your treat. || Food. On you.", "A summer night, a girl in a yukata... You're lucky, you know~ || Summer night, me in yukata. Lucky boy. || You're lucky tonight. || Me in yukata. Appreciate it."));
                b.Choice("Goldfish scooping", "tb_fish", "Shaved ice", "tb_ice", "Walk under the lanterns", "tb_walk");
                b.Branch("tb_fish");
                b.Narr("Your paper net tears on the first try. Hers tears on the second. The old man gives you one goldfish out of pity.");
                b.Say(g, T(g, "He's so small... Can I keep him? I'll name him after you. || He's tiny... I'll name him after you. || So small... can I keep him? || A little goldfish... I'll love him.", "WE CAUGHT ONE! Kind of! He counts! || CAUGHT ONE! Kinda! || We got one! Sorta! || Success! A fish!", "...I'll take care of him. Since you can't. || I'll care for him. || ...I'll raise him. || ...He's mine now.", "Our first pet together~ || Our first pet~ || Our baby fish~ || A pet together. Cute."));
                b.Goto("tb_wish");
                b.Branch("tb_ice");
                b.Narr("Strawberry for her, melon for you. Brain freeze for both.");
                b.Say(g, T(g, "Want to try mine...? It's strawberry. || Try mine? Strawberry. || Want some? It's strawberry. || Here... taste?", "Ow ow ow, my head! Worth it! || Brain freeze! Worth it! || Ow! Cold! Yum! || Headache! Delicious!", "Your tongue is green. Very dignified. || Green tongue. Dignified. || ...Your tongue is green. || ...Colorful tongue.", "Indirect kiss~ Just kidding. Unless...? || Indirect kiss~ Kidding. Unless? || Share? Indirect kiss. || Taste mine~"));
                b.Goto("tb_wish");
                b.Branch("tb_walk");
                b.Narr("You walk slowly under the paper lanterns. The crowd pushes you closer together. Neither of you steps away.");
                b.Goto("tb_wish");

                b.Label("tb_wish");
                b.Bg("tanabata_street");
                b.Narr("On the way back you pass her bamboo branch. Her paper strip has come loose and flutters down at your feet.");
                if (opens)
                {
                    b.Narr("You can't help reading it.");
                    b.Narr(T(g, "\"I hope he notices how I feel.\" || \"I hope he notices me.\" || \"Please let him see how I feel.\" || \"Let him understand my heart.\"", "\"Let me spend every summer with him!\" || \"Every summer with him!\" || \"More summers together!\" || \"Him! Always!\"", "\"I want to be honest with him. Just once.\" || \"Let me be honest with him.\" || \"Honesty, once.\" || \"Courage to tell him.\"", "\"Make him fall for me. Hard.\" || \"Make him fall for me.\" || \"Him. Mine. Hard.\" || \"Make him crazy about me.\""));
                    b.Say(g, T(g, "Y-you read it...! ...Well? Is it going to come true? || You read it! ...Will it come true? || Y-you saw...! Well? || You read it... so?", "AAAH! Forget you saw that! ...Or don't. Whatever! || AAAH! Forget it! Or don't! || Unsee it! ...Or whatever! || Nooo! ...Okay, fine!", "...Now you know. Don't make it weird. || ...Now you know. || ...Don't make it weird. || ...Well. Known.", "Oops~ Well, now you know. What are you going to do about it? || Oops~ Now what? || Read it? Your move. || Now you know. Act."));
                }
                else b.Say(g, T(g, "Don't read it! Please! || Don't read it! || Please, no! || Not mine!", "Mine! Give it! No peeking! || Mine! No peeking! || Give it! || Nope!", "Hand it over. Now. || Hand it over. || Give. || Now.", "Curious? Earn it first~ || Earn it. || Curious? Pay. || Not free."));
                b.Narr("She ties it back up, higher this time.");
            }
            b.Bg("@skyline|tanabata_street");
            b.Narr("Above the street, the Milky Way is clear tonight. Somewhere up there, two lovers are meeting.");

            return Scenes.PlayOnMap(b, _ =>
            {
                var p = Scenes.Picks;
                if (p.Contains("tb_int")) Pay(new Rewards { Int = 3 }, null, "Your Tanabata wish was heard");
                else if (p.Contains("tb_str")) Pay(new Rewards { Str = 3 }, null, "Your Tanabata wish was heard");
                else if (p.Contains("tb_sen")) Pay(new Rewards { Sens = 3 }, null, "Your Tanabata wish was heard");
                else { foreach (var x in TopGirls(5)) G.AddFavor(x, 3, 0); Toasts.Show("Somehow, everyone seems a bit warmer today."); }
                if (g == null) return;
                if (p.Contains("tb_fish")) Pay(new Rewards { Favor = 6, Devotion = 1 }, g, "A goldfish with " + N(g));
                else if (p.Contains("tb_ice")) Pay(new Rewards { Favor = 5, Lewd = 2 }, g, "Shaved ice with " + N(g));
                else if (p.Contains("tb_walk")) Pay(new Rewards { Favor = 6, Lewd = 1 }, g, "A walk under the lanterns with " + N(g));
                if (opens) Pay(new Rewards { Favor = 4, Lewd = 2 }, g, "You read " + N(g) + "'s wish");
            });
        }

        // ================= Halloween =================

        private static bool Halloween()
        {
            var girls = TopGirls(3);
            var pair = girls.FirstOrDefault();
            var sweets = new[] { "chocolate", "cookies", "daifuku" };
            var have = sweets.Sum(x => S.Count(x));
            var charm = Charm();
            var vampireWin = Roll(G.Sens, 60, charm);
            var b = new SceneBuilder(null, false);

            b.Bg("classroom_halloween");
            b.Narr("Halloween. The lecture hall is covered in paper bats, jack-o'-lanterns and orange streamers.");
            b.Narr("There's a costume contest after class, and the course rep says nobody leaves without a costume.");
            b.Choice("Vampire, cape and all [SEN]", "hw_vamp", "A bedsheet ghost", "hw_ghost", "No costume. You're too cool.", "hw_none");
            b.Branch("hw_vamp");
            b.Narr("Fake fangs, a red-lined cape, hair slicked back. In the mirror, you look... surprisingly good?");
            b.Narr(vampireWin ? "At the contest the girls in the front row squeal when you swirl the cape. You win! A paper crown and a box of candy." : "At the contest you trip on the cape during your big entrance. Second place, for the effort.");
            b.Goto("hw_trick");
            b.Branch("hw_ghost");
            b.Narr("A bedsheet with two holes. You bump into three desks on the way to your seat.");
            b.Narr("At the contest the whole class laughs when your ghost walks into the door. Special prize: most dedicated ghost.");
            b.Goto("hw_trick");
            b.Branch("hw_none");
            b.Narr("The course rep sighs and sticks cat ears on your head. They stay there all day.");
            b.Narr("At the contest you're not even entered. The cat ears get three votes anyway.");
            b.Goto("hw_trick");

            b.Label("hw_trick");
            b.Narr("At lunch, the trick-or-treaters arrive.");
            foreach (var g in girls)
                b.Say(g, T(g, "T-trick or treat...? || Trick or treat...? || T-trick or... treat? || Um... trick or treat?", "TRICK OR TREAT! || TRICK OR TREAT! || Candy! Now! || Treat me!", "Trick or treat. Choose wisely. || Trick or treat. Choose. || Choose. || Treat. Or else.", "Trick or treat~ I'm hoping for the trick. || Hoping for the trick~ || Trick or treat? Pick trick. || Treat or trick~"));
            if (have >= girls.Count) b.Narr("You hand out sweets from your bag. Everyone's happy.");
            else
            {
                b.Narr("You don't have enough sweets... Time for some tricks.");
                var trickster = girls.LastOrDefault();
                if (trickster != null)
                    b.Say(trickster, T(trickster, "Then... I'll draw a cat face on your cheek. Hold still... || A cat face on your cheek... hold still. || Then a little doodle... stay still. || Trick! Cat whiskers!", "No candy? TICKLE ATTACK! || TICKLE ATTACK! || No candy?! Tickles! || Trick time!", "No sweets. Then you carry my bag all day. || You carry my bag. All day. || Penalty: bag duty. || Then you're my porter.", "No treats? Then close your eyes~ This trick is a secret. || Close your eyes. Secret trick. || Eyes shut~ || A secret trick. Ready?"));
            }

            b.Bg("dark_hallway");
            b.Narr("After dark: the test of courage. Pairs walk the old hallway with one flashlight and no adults.");
            if (pair != null)
            {
                b.Narr("You draw lots. Your partner is " + N(pair) + ".");
                b.Say(pair, T(pair, "I-it's really dark... You'll stay close, right? || So dark... stay close? || It's scary... don't leave me. || Please stay next to me...", "This is gonna be AWESOME! Let's go! || AWESOME! Go! || Haunted! Fun! || Let's go!", "It's a hallway. Hallways aren't scary. || A hallway. Not scary. || Just a corridor. || Nothing to fear.", "Alone in the dark with you~ Should I be scared of the ghosts, or of you? || Scared of ghosts or you? || Dark and you. Dangerous. || Which is scarier? You."));
                b.Narr("Halfway down, the flashlight flickers. Somewhere behind you, a locker door slowly creaks open.");
                b.Choice("Take her hand", "hw_hand", "Jump out and scare her", "hw_scare", "Walk ahead bravely", "hw_brave");
                b.Branch("hw_hand");
                b.Narr("You find her hand in the dark. She squeezes it so hard it hurts. You don't let go until the end.");
                b.Say(pair, T(pair, "...Thank you. I'm okay now. Just... don't let go. || Thank you... don't let go. || I'm okay now... keep holding. || Better... stay close.", "Ha! I wasn't scared! ...Keep holding it though. || Not scared! ...Keep holding. || Ha! ...Don't let go. || I'm brave! ...Hold on.", "...This is for your sake. You looked scared. || ...For your sake. || ...You looked scared. || ...Purely for you.", "Bold~ Should we walk slower? || Bold. Walk slower? || Slower, maybe? || Holding me? Bold."));
                b.Goto("hw_party");
                b.Branch("hw_scare");
                b.Narr("\"BOO!\" She screams, then punches your arm, then laughs so hard she has to sit down.");
                b.Say(pair, T(pair, "That was so mean! ...My heart's still racing. || Mean! ...My heart's racing. || That was awful! ...Wow. || So scary...! You're mean!", "AHAHA! Okay, okay, you got me! Revenge later! || You got me! Revenge later! || AHAHA! Payback soon! || Okay! You win! For now!", "...I will remember this. || ...Remembered. || ...You'll regret this. || ...Noted.", "Scaring a girl in the dark? You're worse than the ghosts~ || Worse than ghosts. || Scaring me? Bad boy. || You're the real monster."));
                b.Goto("hw_party");
                b.Branch("hw_brave");
                b.Narr("You walk in front, flashlight high. Behind you, she holds the hem of your jacket the whole way.");
                b.Goto("hw_party");
            }

            b.Label("hw_party");
            b.Bg("classroom_halloween");
            b.Narr("Back in the lecture hall, the jack-o'-lanterns are lit for the party.");
            b.Narr("Pumpkin cake, orange juice and the bad horror movie somebody brought. Happy Halloween.");

            return Scenes.PlayOnMap(b, _ =>
            {
                var p = Scenes.Picks;
                UseCharm(charm);
                var left = girls.Count;
                foreach (var x in sweets) while (left > 0 && S.Count(x) > 0) { S.AddItem(x, -1); left--; }
                for (var i = 0; i < girls.Count; i++)
                {
                    if (i < girls.Count - left) G.AddFavor(girls[i], 5, 0);
                    else G.AddFavor(girls[i], 1, 4); // a playful trick
                }
                if (p.Contains("hw_vamp"))
                {
                    if (vampireWin) { Pay(new Rewards { Rep = 5, Sens = 1 }, null, "Your vampire won the costume contest!"); foreach (var x in girls) G.AddFavor(x, 0, 2); }
                    else Pay(new Rewards { Rep = 2 }, null, "Your vampire got second place in the costume contest");
                }
                else if (p.Contains("hw_ghost")) Pay(new Rewards { Rep = 2 }, null, "The bedsheet ghost made everyone laugh");
                if (pair != null)
                {
                    if (p.Contains("hw_hand")) Pay(new Rewards { Favor = 7, Lewd = 3 }, pair, "Hand in hand with " + N(pair) + " in the dark hallway");
                    else if (p.Contains("hw_scare")) Pay(new Rewards { Favor = 2, Lewd = 2 }, pair, "You scared " + N(pair) + " in the test of courage");
                    else if (p.Contains("hw_brave")) Pay(new Rewards { Favor = 5, Rep = 1 }, pair, "You led " + N(pair) + " through the dark hallway");
                }
                Toasts.Show("Happy Halloween!");
            });
        }

        // ================= Field trip =================

        private static bool Trip()
        {
            var kyoto = Random.value < 0.5f;
            var tops = TopGirls(2);
            var b = new SceneBuilder(null, false);

            b.Bg("@station2|@station|train");
            b.Narr("Field trip day! The whole class boards the bullet train to " + (kyoto ? "Kyoto" : "a hot spring town in the mountains") + ".");
            b.Bg("train");
            b.Narr("Rice fields fly past the window. Somebody already opened the snacks. The professor has given up.");
            b.Narr("There's a free seat next to you.");
            var opts = new List<string>();
            for (var i = 0; i < tops.Count; i++) { opts.Add("Wave " + N(tops[i]) + " over"); opts.Add("tr_g" + i); }
            opts.Add("Keep the window seat to yourself"); opts.Add("tr_solo");
            b.Choice(opts.ToArray());

            for (var i = 0; i < tops.Count; i++)
            {
                var g = tops[i];
                var k = "tr_g" + i;
                b.Branch(k);
                b.Say(g, T(g, "You saved me a seat...? Thank you. || You saved me a seat? Thanks. || A seat for me... thank you. || You kept it for me...", "Window seat neighbor! Want some chips? || Window buddy! Chips?! || Seatmates! Snacks! || Neighbors! Yay!", "Fine. Don't hog the armrest. || Don't hog the armrest. || Armrest is mine. || Fine. Share fairly.", "Two hours sitting next to me. Lucky you~ || Two hours with me. Lucky. || Sitting with me? Fortunate. || Enjoy my company."));
                b.Narr("You play cards for an hour. She cheats. You let her.");
                if (kyoto)
                {
                    b.Bg("kyoto_temple");
                    b.Narr("Kyoto. Temples, old streets and far too many souvenir shops. The class splits up for free time.");
                    b.Say(g, T(g, "Just the two of us? I was hoping you'd ask... || Just us? I hoped you'd ask. || The two of us? Yes... || I wanted that too...", "Yay! Let's go everywhere! || Everywhere! Go! || Yay! Explore! || Let's go!", "I suppose you can come along. || You may come. || Fine. Join. || Acceptable.", "Sneaking away with me? I like it~ || Sneaking off? I like it. || Escaping? Fun. || Just us? Naughty."));
                    b.Choice("Rent kimonos and walk the old streets", k + "_a", "Write a wish on an ema together", k + "_b");
                    b.Branch(k + "_a");
                    b.Say(g, T(g, "How do I look...? You're staring. || How do I look? You're staring. || Is this okay...? || Do I look nice?", "Look, look! I'm a real princess now! || Princess mode! || Look! Royal! || I'm a princess!", "Stop taking pictures. ...Fine, one more. || One more photo. Last. || Stop. ...One more. || Enough. ...Okay, one.", "Help me with the obi? It's so hard to tie alone~ || Help with the obi? || Tie this for me? || Obi trouble. Help?"));
                    b.Goto(k + "_inn");
                    b.Branch(k + "_b");
                    b.Say(g, T(g, "Don't read mine! ...It's about you. A little. || Don't read it! ...It's about you. || It's... a little about you. || Please don't look...", "I wished for ten more trips like this one! || Ten more trips! || More trips like this! || Again and again!", "Wishes are childish. ...I wrote one anyway. || Childish. ...I wrote one. || Silly. ...I did too. || ...Mine's private.", "Mine says 'that he notices me'. Oops, you read it~ || 'That he notices me.' Oops. || Read it? Oops. || My wish? You saw."));
                    b.Goto(k + "_inn");
                }
                else
                {
                    b.Bg("inn_room_day");
                    b.Narr("The inn is old and beautiful: tatami rooms, sliding doors, a garden with a stone lantern. Steam rises behind the bamboo fence.");
                    b.Say(g, T(g, "It's so pretty here... I'm glad I came with you. || It's beautiful... glad I came with you. || So pretty... thank you for coming. || What a lovely place...", "Hot springs! Ping-pong! Milk in glass bottles! Let's go! || Hot springs! Ping-pong! Milk! || Onsen time! || Bath! Games! Snacks!", "Quiet. Good. I like this place. || Quiet. Good. || Peaceful. I like it. || Calm. Pleasant.", "Mixed bathing is around the back, I heard~ Just saying. || Mixed bath's out back. Just saying. || Heard there's mixed bathing~ || The baths... interesting."));
                    var moments = new List<string> { "Play ping-pong in yukata", k + "_a", "Sit in the garden together", k + "_b" };
                    if (G.Sens >= 50) { moments.Add("Peek over the bamboo fence later [SEN]"); moments.Add(k + "_peek"); }
                    b.Choice(moments.ToArray());
                    b.Branch(k + "_a");
                    b.Say(g, T(g, "Ah, my yukata is coming loose... D-don't look! || My yukata's loose! Don't look! || Ah! Don't look! || Eek! My collar!", "SMASH! Ha! Another one, loser buys milk! || SMASH! Loser buys milk! || Point! Again! || Ha! Winner!", "You're going down. ...Stop looking at my collar. || You're going down. Eyes up. || Focus. Not my collar. || Watch the ball.", "If I win, you do whatever I say tonight~ || If I win, you obey tonight. || Win and you're mine tonight. || Stakes: tonight. Ready?"));
                    b.Goto(k + "_inn");
                    b.Branch(k + "_b");
                    b.Say(g, T(g, "It's so quiet... I can hear the water. I like being here with you. || So quiet... I like being here with you. || Peaceful... with you. || Listen... the water.", "Look! A frog! A HUGE frog! || A frog! Huge! || Look! Fish! || Wow! Frog!", "...This is nice. Don't ruin it by talking. || ...Nice. Don't talk. || ...Silence. Good. || ...Peaceful.", "A garden, a quiet afternoon, just us~ What are you thinking about? || Just us. What are you thinking? || Quiet afternoon. Thoughts? || A garden, you, me. Hmm."));
                    b.Goto(k + "_inn");
                    b.Branch(k + "_peek");
                    b.Narr("While the others nap, you climb the rocks behind the bamboo fence, holding your breath...");
                    b.Goto(k + "_inn");
                }

                b.Label(k + "_inn");
                b.Bg("inn_room_night");
                b.Narr("Night at the inn. Lights out at ten, the professor said. It's eleven-thirty and nobody is asleep.");
                b.Narr("A soft knock on the sliding door. It's " + N(g) + ", in her yukata, whispering.");
                b.Say(g, T(g, "I couldn't sleep... Can I stay for a bit? || I couldn't sleep... can I stay? || Sleepless... may I sit here? || I wanted to see you...", "Psst! The girls are having a pillow fight, I escaped! Hide me! || Pillow fight! I escaped! Hide me! || Shh! Hide me! || Escaped! Cover me!", "My roommates snore. I'm staying here for ten minutes. || Roommates snore. Ten minutes. || Too noisy there. Ten minutes. || I'll stay briefly.", "Sneaking into the boys' room~ If we get caught, it's your fault. || If we're caught, your fault. || Sneaking in. Exciting. || Boys' room. Risky."));
                b.Narr("Footsteps in the corridor. The professor's flashlight sweeps under the door.");
                b.Choice("Hide her in the closet", k + "_closet", "Talk quietly until it passes", k + "_talk", "Send her back before you're caught", k + "_back");
                b.Branch(k + "_closet");
                b.Narr("You both squeeze into the futon closet. It's very dark and very small. You can hear her heartbeat. Or yours.");
                b.Say(g, T(g, "...Your heart is so loud. || ...Your heart's so loud. || ...I can hear your heart. || ...So close...", "Mmmph! Don't make me laugh! || Mmph! Don't make me laugh! || Shh! Giggles! || Hmph! Quiet!", "...Don't move. Don't breathe. Don't think. || ...Don't move. || ...Silence. || ...Stay still.", "Cozy~ We should get caught more often. || Cozy. Get caught more often. || We should hide more. || Snug~"));
                b.Goto("tr_home");
                b.Branch(k + "_talk");
                b.Narr("The footsteps pass. You talk in whispers until two in the morning: families, dreams, the stupid things you did as kids.");
                b.Goto("tr_home");
                b.Branch(k + "_back");
                b.Narr("You slide the door open just in time. She tiptoes back. The professor only sees you, waving awkwardly.");
                b.Goto("tr_home");
            }

            b.Branch("tr_solo");
            b.Narr("You watch the mountains go by and listen to music. Peaceful.");
            b.Bg(kyoto ? "kyoto_temple" : "inn_room_day");
            b.Narr(kyoto ? "In Kyoto you wander the temples on your own and buy a lucky charm (omamori) at the gate." : "At the inn you soak in the hot spring until you're a boiled noodle, then buy a lucky charm at the shrine.");
            b.Goto("tr_home");

            b.Label("tr_home");
            b.Bg("train");
            b.Narr("The train home. Everyone is exhausted. The car is quiet except for snoring.");
            if (tops.Count > 0) b.Narr("Something warm settles on your shoulder. Somebody fell asleep on you. You don't move for the rest of the trip.");
            b.Narr("A trip to remember.");

            return Scenes.PlayOnMap(b, _ =>
            {
                var p = Scenes.Picks;
                if (p.Contains("tr_solo"))
                {
                    S.AddItem("omamori", 1);
                    Pay(new Rewards { Int = 1 }, null, "You bought a lucky charm (omamori) as a souvenir");
                    return;
                }
                for (var i = 0; i < tops.Count; i++)
                {
                    var k = "tr_g" + i;
                    if (!p.Contains(k)) continue;
                    var g = tops[i];
                    if (p.Contains(k + "_peek"))
                    {
                        int roll;
                        if (Campus.Check(G.Sens, 80, out roll)) { Pay(new Rewards { Sens = 3 }, null, "You saw... paradise. Nobody noticed"); foreach (var x in tops) G.AddFavor(x, 0, 3); }
                        else { G.AddFavor(g, -5, 2); Pay(new Rewards { Rep = -6, Sens = 1 }, null, "Caught peeking! A bucket flies at your head"); }
                    }
                    else
                    {
                        var flirty = p.Contains(k + "_a");
                        Pay(new Rewards { Favor = flirty ? 8 : 11, Lewd = (kyoto ? 1 : 4) + (flirty ? 3 : 0) }, g, "A trip to remember with " + N(g));
                    }
                    if (p.Contains(k + "_closet")) Pay(new Rewards { Favor = 5, Lewd = 6 }, g, "Hiding in the closet with " + N(g));
                    else if (p.Contains(k + "_talk")) Pay(new Rewards { Favor = 7, Lewd = 1, Devotion = 2 }, g, "Talking with " + N(g) + " until two in the morning");
                    else if (p.Contains(k + "_back")) Pay(new Rewards { Favor = 2, Rep = 1 }, g, N(g) + " made it back unseen");
                }
            });
        }

        // ================= Christmas Eve =================

        private static bool Xmas()
        {
            var gf = Board.Students.Where(h => h.isGirlfriend).OrderByDescending(h => h.favor).FirstOrDefault();
            var b = new SceneBuilder(null, false);
            SaveData.Heroine texter = null;

            if (gf != null)
            {
                b.Bg("snow_street_night");
                b.Narr("Christmas Eve. Snow on the rooftops, lights in every window, couples on every corner.");
                b.Narr("For once, you're one of them. " + N(gf) + " is waiting under the streetlight, her breath white in the cold.");
                b.Say(gf, T(gf, "You came... Merry Christmas. I've been looking forward to this all month. || You came... Merry Christmas. || Merry Christmas... I waited all month. || I'm so glad you came. Merry Christmas.", "MERRY CHRISTMAS! Look, it's snowing! It's actually snowing! || MERRY CHRISTMAS! Snow! || It's snowing! Merry Christmas! || Christmas! Yay!",
                    "You're late. ...By two minutes. I'll allow it. Merry Christmas. || Late. Two minutes. Merry Christmas. || Merry Christmas. You're late. Slightly. || ...Merry Christmas.", "Merry Christmas~ Did you dress up for me? You did, didn't you. || Dressed up for me? You did. || Merry Christmas~ Looking sharp. || Did you dress for me? Cute."));
                b.Narr("You walk through the quiet streets. She slips her hand into your coat pocket, next to yours.");
                b.Bg("snow_park_night");
                b.Narr("The park is empty. Fresh snow on the bench, one lamp glowing gold. It feels like the whole world is holding its breath.");
                b.Narr("You take out the present you've been hiding all evening.");
                b.Choice("A warm scarf", "xm_scarf", "A small necklace", "xm_necklace", "A handmade card", "xm_card");
                b.Branch("xm_scarf");
                b.Narr("You wrap it around her neck yourself. It covers her up to the nose.");
                b.Say(gf, T(gf, "It's so warm... It smells like you. || So warm... smells like you. || Warm... thank you. || Cozy... I love it.", "SO FLUFFY! I'm never taking it off! || FLUFFY! Never removing it! || So soft! || Warm! Yay!", "...Practical. I like practical. ...I like it a lot. || Practical. I like it. A lot. || ...Useful. Thank you. || ...I'll wear it daily.", "Wrapping me up like a present? Who's unwrapping me later~? || Wrapping me up? Unwrapping later? || Gift-wrapped me? Bold. || Who unwraps me?"));
                b.Goto("xm_hers");
                b.Branch("xm_necklace");
                b.Narr("Her eyes go wide when she opens the little box. Her hands are shaking as you fasten it.");
                b.Say(gf, T(gf, "It's beautiful... I'll treasure it forever. || Beautiful... I'll treasure it. || It's lovely... thank you. || So pretty... forever.", "Shiny! It's so pretty! Was it expensive?! Don't tell me! || Shiny! Don't tell me the price! || Pretty! Wow! || Sparkly!", "...You didn't have to. ...I'm wearing it every day. || ...Every day. I'll wear it. || ...Unnecessary. Thank you. || ...Beautiful.", "Jewelry? Someone's serious about me~ || Jewelry? Serious, are we? || Someone's committed. || Ooh, jewelry. Serious."));
                b.Goto("xm_hers");
                b.Branch("xm_card");
                b.Narr("A card you drew yourself, a little crooked, with everything you never managed to say out loud.");
                b.Say(gf, T(gf, "...I'm crying. I'm sorry. It's the best present ever. || I'm crying... best gift ever. || Sorry... tears. It's perfect. || I love it so much...", "You DREW this?! It's so bad! I love it! || You DREW this?! So bad! Love it! || Handmade?! Amazing! || I love it! Ugly! Perfect!", "...I'll read it again. Later. Alone. || ...Later. Alone. I'll reread. || ...I'll keep it. || ...Private reading later.", "Such sweet words~ I'm keeping this to tease you for years. || Sweet words. I'll tease you for years. || I'm keeping this. Blackmail. || Adorable. Saving forever."));
                b.Goto("xm_hers");
                b.Label("xm_hers");
                b.Say(gf, T(gf, "This is from me... Open it. || From me... open it. || This is for you... please. || My gift... open it.", "My turn! Open it, open it! || My turn! Open! || Open it! Now! || Your present! Go!", "Here. It's nothing special. ...Open it. || Nothing special. Open it. || Here. Open. || ...Yours.", "My present is in this box. The other one comes later~ || The other gift comes later. || Box one. Box two later. || Part one. Part two tonight."));
                b.Narr("Gloves. Hand-knitted, a little uneven. One finger is longer than the others. They're perfect.");
                b.Bg("bedroom_snow_night");
                b.Narr("Later. Your room, the lights off, snow falling past the window.");
                b.Say(gf, T(gf, "Merry Christmas... I'm so happy to spend it with you. || Merry Christmas... I'm so happy. || So happy to spend it with you. || Merry Christmas, my love.", "Merry Christmas! Best. Christmas. Ever. || Best Christmas ever! || Merry Christmas! Woo! || Greatest Christmas!", "Merry Christmas. ...Stay with me tonight. || Merry Christmas. Stay tonight. || ...Stay with me. || ...Merry Christmas. Don't go.", "Merry Christmas~ Your last present is... me. Kidding. Maybe. || Last present: me. Maybe. || Final gift? Me. Kidding. || Unwrap me later. Joking. Maybe."));
                b.Narr("Outside, the snow keeps falling. Inside, nobody is cold anymore.");
            }
            else
            {
                b.Bg("classroom_sunset");
                b.Narr("Christmas Eve. Lights on every street, couples everywhere.");
                b.Narr("You spend it with the Koikatsu Club: a cake too big for the club room, paper hats, and a gift exchange.");
                b.Narr("You get socks. Somebody else gets your socks. It's that kind of party.");
                b.Bg("snow_street_night");
                b.Narr("On the way home the snow starts. The street is quiet and the houses glow warm behind their curtains.");
                texter = Board.Students.Where(h => h.favor >= 40).OrderByDescending(h => h.favor).FirstOrDefault();
                if (texter != null)
                {
                    b.Narr("Your phone buzzes. A message from " + N(texter) + ".");
                    b.Say(texter, T(texter, "Merry Christmas... I just wanted to say it to you first. || Merry Christmas... I wanted to tell you first. || I wanted you to be first... Merry Christmas. || Merry Christmas... to you, first.", "MERRY CHRISTMAS!!! Save me some cake!! || MERRY CHRISTMAS! Cake! || Christmas! Save cake! || Yay! Merry Christmas!",
                        "Merry Christmas. That's all. ...Don't read into it. || Merry Christmas. That's all. || ...Merry Christmas. Don't read into it. || ...Season's greetings.", "Merry Christmas~ Next year, keep the evening free for me. || Next year, keep the evening free. || Reserve next year for me. || Next Christmas is mine."));
                    if (texter.favor >= 60)
                    {
                        b.Narr("Another buzz. \"...I'm at the park near the station. If you're close.\"");
                        b.Choice("Run to the park", "xm_go", "Reply and go home", "xm_reply");
                        b.Branch("xm_go");
                        b.Bg("snow_park_night");
                        b.Narr("She's on the bench under the lamp, snow on her hair, holding two cans of hot cocoa. One for you.");
                        b.Say(texter, T(texter, "You actually came... Merry Christmas. For real this time. || You came... Merry Christmas. Truly. || You're here... Merry Christmas. || I knew you'd come...", "YOU CAME! I bet myself you would! || YOU CAME! I knew it! || You're here! I bet on it! || Yay! You came!", "...You're out of breath. Idiot. Sit. || ...Out of breath. Sit. || ...Idiot. Rest. || ...You ran. Sit.", "Running to me on Christmas Eve? That's practically a confession~ || Running to me? A confession. || Rushing to me? Telling. || You ran? So obvious."));
                        b.Narr("You sit together until the cocoa goes cold, watching the snow.");
                        b.Goto("xm_end");
                        b.Branch("xm_reply");
                        b.Me("Merry Christmas to you too. See you on campus.");
                        b.Goto("xm_end");
                        b.Label("xm_end");
                    }
                }
                b.Narr("Next year will be different.");
            }

            return Scenes.PlayOnMap(b, _ =>
            {
                var p = Scenes.Picks;
                if (gf != null)
                {
                    if (p.Contains("xm_scarf")) Pay(new Rewards { Favor = 8, Lewd = 4 }, gf, "A scarf for " + N(gf));
                    else if (p.Contains("xm_necklace")) Pay(new Rewards { Favor = 10, Lewd = 4, Money = -3000 }, gf, "A necklace for " + N(gf));
                    else Pay(new Rewards { Favor = 12, Lewd = 2, Devotion = 3 }, gf, "A handmade card for " + N(gf));
                    G.AddFavor(gf, 0, 6);
                    foreach (var other in Board.Students.Where(h => h.isGirlfriend && h != gf)) G.AddFavor(other, -3, 0);
                    Toasts.Show("A Christmas to remember.");
                    return;
                }
                Pay(new Rewards { Club = 2, Rep = 1 }, null, "Club Christmas party");
                if (texter == null) return;
                if (p.Contains("xm_go")) Pay(new Rewards { Favor = 10, Lewd = 3, Devotion = 2 }, texter, "Christmas Eve in the snow with " + N(texter));
                else Pay(new Rewards { Favor = 4, Lewd = 1 }, texter, "A Christmas message from " + N(texter));
            });
        }
    }
}
