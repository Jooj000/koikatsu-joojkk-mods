// The request talks, one per kind.
using System.Collections.Generic;

namespace Joojkk.CampusLife
{
    internal static partial class Conversation
    {
    /// <summary>One request talk per kind: your opening line and four options, each with her answer by tone.</summary>
    private static class RequestTalks
    {
        internal sealed class Opt { public string Tag, Label; public Q Q; public bool BadWhenLow; public string[] Her; }
        internal sealed class Talk { public string[] Me; public Opt[] Opts; }

        internal static readonly Dictionary<RequestKind, Talk> ByKind = new Dictionary<RequestKind, Talk>
        {
            { RequestKind.Lunch, new Talk { Me = new[] { "I saw your note about lunch.", "You're the one looking for a lunch buddy, right?" }, Opts = new[] {
                new Opt { Tag = "rq_feel", Label = "Offer to share your lunch with her", Q = Q.Good, BadWhenLow = false, Her = new[] { "Really? You'd split yours with me...?", "Sharing?! You're the best!", "...Half is plenty. Thanks.", "Feeding me? How generous~" } },
                new Opt { Tag = "rq_ask", Label = "Ask what she feels like eating", Q = Q.Good, BadWhenLow = false, Her = new[] { "Something warm... anything, really.", "Everything! Meat! Bread! Everything!", "...Something simple. Rice is fine.", "Whatever you're buying me~" } },
                new Opt { Tag = "rq_promise", Label = "Say you'll sit with her at lunch", Q = Q.Good, BadWhenLow = false, Her = new[] { "Then I won't be alone... I'll save you a seat.", "Yes! Lunch date! I mean, lunch!", "...Fine. Don't be late.", "A lunch date. I'll tell everyone~" } },
                new Opt { Tag = "rq_joke", Label = "Joke that she can have your vegetables", Q = Q.Good, BadWhenLow = true, Her = new[] { "Ehe... I'll take them. I like vegetables.", "Deal! Give me ALL the vegetables!", "...You just don't want them.", "Using me as a trash can? Rude~" } }
            } } },
            { RequestKind.Study, new Talk { Me = new[] { "I saw you're struggling in class.", "You wanted a study partner?" }, Opts = new[] {
                new Opt { Tag = "rq_feel", Label = "Tell her everyone falls behind sometimes", Q = Q.Good, BadWhenLow = false, Her = new[] { "...Really? I thought it was just me.", "Right?! It's not just me!", "...I suppose. Still, I hate it.", "Even you? That makes me feel better~" } },
                new Opt { Tag = "rq_ask", Label = "Ask which part lost her", Q = Q.Good, BadWhenLow = false, Her = new[] { "From the second half... the formulas.", "All of it! Every single page!", "...Chapter four. The proofs.", "The part where the teacher started talking~" } },
                new Opt { Tag = "rq_promise", Label = "Offer to go over it together", Q = Q.Good, BadWhenLow = false, Her = new[] { "Together...? I'd study harder with you.", "Yes! Study party!", "...That would help. Thank you.", "You, me, and a textbook. Romantic~" } },
                new Opt { Tag = "rq_joke", Label = "Joke that the test is scared of her", Q = Q.Good, BadWhenLow = true, Her = new[] { "Ehe... I wish it was.", "Ha! It should be!", "...It isn't. Not yet.", "It will be, after you tutor me~" } }
            } } },
            { RequestKind.Exercise, new Talk { Me = new[] { "You wanted someone to train with?", "I saw you're looking for a workout partner." }, Opts = new[] {
                new Opt { Tag = "rq_feel", Label = "Say you could use the exercise too", Q = Q.Good, BadWhenLow = false, Her = new[] { "Then we can be bad at it together...", "Great! Two is better than one!", "...Good. You look like you need it.", "You do look a little soft~" } },
                new Opt { Tag = "rq_ask", Label = "Ask what kind of training she wants", Q = Q.Good, BadWhenLow = false, Her = new[] { "Something easy... maybe a jog?", "Running! Jumping! Everything!", "...Laps. Then stretching.", "Something that makes you sweat~" } },
                new Opt { Tag = "rq_promise", Label = "Say you'll train with her today", Q = Q.Good, BadWhenLow = false, Her = new[] { "You will...? I'll try not to slow you down.", "Yes! Don't quit on me!", "...Fine. Keep up.", "Don't cry when I win~" } },
                new Opt { Tag = "rq_joke", Label = "Joke that you'll cheer from the bench", Q = Q.Good, BadWhenLow = true, Her = new[] { "That's not training... ehe.", "No way! You're running too!", "...Useless.", "Then I'll drag you off it~" } }
            } } },
            { RequestKind.Club, new Talk { Me = new[] { "You said you had something planned for the club?", "I saw your note about the club." }, Opts = new[] {
                new Opt { Tag = "rq_feel", Label = "Say the club's lucky to have her", Q = Q.Good, BadWhenLow = false, Her = new[] { "I just want everyone to have fun...", "Right?! Somebody has to do it!", "...Somebody has to keep it alive.", "Lucky? Obviously~" } },
                new Opt { Tag = "rq_ask", Label = "Ask what she has in mind", Q = Q.Good, BadWhenLow = false, Her = new[] { "Something small... a little event, maybe.", "A huge event! The biggest!", "...A proper project. I have notes.", "It's a secret until you agree~" } },
                new Opt { Tag = "rq_promise", Label = "Offer to lend a hand", Q = Q.Good, BadWhenLow = false, Her = new[] { "You'd help? Thank you... really.", "Yes! You're in! No take-backs!", "...Good. I'll put you to work.", "I'll work you hard~" } },
                new Opt { Tag = "rq_joke", Label = "Joke that you're joining just for the snacks", Q = Q.Good, BadWhenLow = true, Her = new[] { "Ehe... there are snacks.", "There are SO many snacks!", "...There are no snacks.", "Snacks are for members who work~" } }
            } } },
            { RequestKind.WalkHome, new Talk { Me = new[] { "You don't want to walk home alone today?", "I saw your note about the way home." }, Opts = new[] {
                new Opt { Tag = "rq_feel", Label = "Tell her she doesn't have to walk alone", Q = Q.Good, BadWhenLow = false, Her = new[] { "...Thank you. I feel safer already.", "Yay! Company!", "...I appreciate it.", "My hero~" } },
                new Opt { Tag = "rq_ask", Label = "Ask which way she usually goes", Q = Q.Good, BadWhenLow = false, Her = new[] { "Past the station... then the long road by the river.", "The way with the snack shop!", "...The main road. It's lit.", "The long way. Obviously~" } },
                new Opt { Tag = "rq_promise", Label = "Say you'll walk her home after school", Q = Q.Good, BadWhenLow = false, Her = new[] { "Then I'll wait for you at the gate...", "After school! Don't forget!", "...After school, then. At the gate.", "I'll wait at the gate. Don't keep me waiting~" } },
                new Opt { Tag = "rq_joke", Label = "Joke that you'll protect her from stray cats", Q = Q.Good, BadWhenLow = true, Her = new[] { "Ehe... the cats are nice, though.", "You'd lose to a cat!", "...You'd run.", "You? Protect me? Cute~" } }
            } } },
            { RequestKind.Gift, new Talk { Me = new[] { "I heard you're having a rough day.", "Your note said you could use some cheering up." }, Opts = new[] {
                new Opt { Tag = "rq_feel", Label = "Say she deserves something nice today", Q = Q.Good, BadWhenLow = false, Her = new[] { "...You think so? That's kind of you.", "I do! I really do!", "...Maybe I do.", "Finally, someone gets it~" } },
                new Opt { Tag = "rq_ask", Label = "Ask what would cheer her up", Q = Q.Good, BadWhenLow = false, Her = new[] { "Something sweet... or just something small.", "Snacks! Anything with sugar!", "...Something warm to drink, maybe.", "Surprise me. Make it good~" } },
                new Opt { Tag = "rq_promise", Label = "Promise to bring her something later", Q = Q.Good, BadWhenLow = false, Her = new[] { "You don't have to... but I'll look forward to it.", "Really?! I'll be waiting!", "...I won't hold my breath. ...I will a little.", "I'll be counting the minutes~" } },
                new Opt { Tag = "rq_joke", Label = "Joke that a smile is free", Q = Q.Good, BadWhenLow = true, Her = new[] { "Ehe... that one worked a little.", "Ha! Nice try!", "...Cheap.", "Cheapskate~" } }
            } } },
            { RequestKind.MeetAt, new Talk { Me = new[] { "You wanted to meet me somewhere?", "Your note said to come find you." }, Opts = new[] {
                new Opt { Tag = "rq_feel", Label = "Say you'd like that", Q = Q.Good, BadWhenLow = false, Her = new[] { "Me too... I'll be there.", "Great! It's a plan!", "...Good.", "Of course you would~" } },
                new Opt { Tag = "rq_ask", Label = "Ask what's at the {place}", Q = Q.Good, BadWhenLow = false, Her = new[] { "It's quiet there... I like talking there.", "It's the best spot! You'll see!", "...You'll see when you get there.", "Me. I'll be at the {place}~" } },
                new Opt { Tag = "rq_promise", Label = "Say you'll meet her at the {place}", Q = Q.Good, BadWhenLow = false, Her = new[] { "Then I'll wait for you there.", "The {place}! Don't forget!", "...The {place}, then.", "Don't keep me waiting at the {place}~" } },
                new Opt { Tag = "rq_joke", Label = "Joke that it sounds like a secret meeting", Q = Q.Good, BadWhenLow = true, Her = new[] { "Ehe... a little secret.", "A secret mission!", "...It's not.", "Maybe it is~" } }
            } } },
            { RequestKind.Errand, new Talk { Me = new[] { "You needed a favor?", "I saw your note asking for help." }, Opts = new[] {
                new Opt { Tag = "rq_feel", Label = "Tell her it's no trouble at all", Q = Q.Good, BadWhenLow = false, Her = new[] { "...You're so kind. Thank you.", "Really?! You're a lifesaver!", "...Thanks. I owe you.", "I knew I could count on you~" } },
                new Opt { Tag = "rq_ask", Label = "Ask how soon she needs it", Q = Q.Good, BadWhenLow = false, Her = new[] { "Today, if you can... no rush, though.", "As soon as possible! Like, now!", "...Today. Before the last bell.", "Yesterday. Hurry~" } },
                new Opt { Tag = "rq_promise", Label = "Promise to take care of it", Q = Q.Good, BadWhenLow = false, Her = new[] { "Thank you... I'll pay you back somehow.", "You're the best! Seriously!", "...Good. I'll remember this.", "Good. I'll reward you~" } },
                new Opt { Tag = "rq_joke", Label = "Joke that you'll charge her for it", Q = Q.Good, BadWhenLow = true, Her = new[] { "Ehe... I can pay in snacks.", "Ha! I'm broke!", "...Typical.", "I'll pay you in a different way~" } }
            } } },
            { RequestKind.Talk, new Talk { Me = new[] { "You wanted to talk?", "Your note said you had something to tell me." }, Opts = new[] {
                new Opt { Tag = "rq_feel", Label = "Say you always have time for her", Q = Q.Good, BadWhenLow = false, Her = new[] { "...You always say the right thing.", "Yay! You're the best!", "...Good.", "Always? I'll remember that~" } },
                new Opt { Tag = "rq_ask", Label = "Ask what's on her mind", Q = Q.Good, BadWhenLow = false, Her = new[] { "It's... a little embarrassing. Later, okay?", "So much! Where do I start?!", "...It can wait until later.", "You'll find out later~" } },
                new Opt { Tag = "rq_promise", Label = "Say you'll find her later", Q = Q.Good, BadWhenLow = false, Her = new[] { "Then I'll wait for you...", "Later! You promised!", "...Later, then.", "Don't make me come find you~" } },
                new Opt { Tag = "rq_joke", Label = "Joke that it sounds serious", Q = Q.Good, BadWhenLow = true, Her = new[] { "Ehe... it's not that serious.", "It's SUPER serious! Kidding!", "...Maybe it is.", "Very serious. Very private~" } }
            } } },
            { RequestKind.Intimate, new Talk { Me = new[] { "About what you said earlier...", "I got your message." }, Opts = new[] {
                new Opt { Tag = "rq_feel", Label = "Tell her you've been thinking about her too", Q = Q.Good, BadWhenLow = false, Her = new[] { "...Me too. A lot.", "Really?! Me too!", "...Good. Then we agree.", "Of course you have~" } },
                new Opt { Tag = "rq_ask", Label = "Ask where she wants to go", Q = Q.Good, BadWhenLow = false, Her = new[] { "Somewhere quiet... where no one comes.", "Anywhere! Right now!", "...Somewhere private. You know where.", "Somewhere no one will hear~" } },
                new Opt { Tag = "rq_promise", Label = "Say you'll find a quiet moment later", Q = Q.Good, BadWhenLow = false, Her = new[] { "...I'll be waiting. Don't make me wait long.", "Later! Promise!", "...Later. I'll hold you to it.", "Don't make me wait~" } },
                new Opt { Tag = "rq_joke", Label = "Joke that she's being bold today", Q = Q.Good, BadWhenLow = true, Her = new[] { "I-I'm not! ...Maybe a little.", "Is that bad?!", "...Don't make it weird.", "You like it~" } }
            } } },
        };
    }
    }
}
