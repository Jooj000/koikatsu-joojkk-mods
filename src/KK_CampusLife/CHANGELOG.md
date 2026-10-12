# Campus Life changelog

Until 2.5 the plugin's own version (`CampusLifePlugin.Version`) stayed at 2.0.0 and the numbers were the milestones of the combined repository. From 3.0.0 on the plugin carries the version itself; the cycles between 2.5 and 3.0 are numbered 2.6 to 2.9 below.

## 3.5.0
- Professors and the nurse act like staff. They sit at the faculty desks, work in the infirmary and walk the halls, and never take a student's desk.
- Talking to a professor at her spot opens the mod's procedural conversation in place of the game's repeating one. Students, professors and the nurse each have their own options and first lines.
- Each professor speaks by her own personality, with an opening line of her own.
- Calendar events keep their nature: Chocolate Day is a day for giving chocolate, an exam is an exam, a trip is a trip. None of them is called a party anymore.
- A dialogue review fixed lines and options that did not follow what was said before them.
- The uniform is back: the "College clothes" option from 3.4.0 is gone, so the outfits of the card show as the game places them.
- With a class over the daily limit, the director brings fewer professors and story heroines to campus. Only those with an event that day are sure to come; once they are regular students they join the normal selection.
- The girl you are courting comes back the next day: anyone you talked to in the last three days now has priority in the daily selection.

## 3.4.0
- The setting is a college. Every line, option, label, tip and option description now talks about a university, professors, the dean and the campus; the game's own staff titles follow through `data\translation\zzz_JoojkkCampus.txt`. The game's own story text is not rewritten.
- College clothes (option, on by default): on campus everyone wears the Casual outfit of their card instead of the School outfits.
- Professors have spicier lines in a flirty mood, in place of the old playful ones.
- The nurse's flirt scene happens in the infirmary, and lines about desks and class belong to students.
- "Invite her to H" with a wary no now says what held her back, like "Sneak off together".
- Every option now belongs to the line it answers. Each like, each subject and each thing she brings up (club, weather, a place, a rumor, a fight with a friend, an exam, a professor's or the nurse's job) has its own options and its own answers, written together, and what you say first leads to what she says.

## 3.3.1
- The school nurse has her own job. Her moods come from the infirmary (quiet days, supplies, records, checkups, students faking a fever), her lines and options talk about it, and the Actions list offers "Say you don't feel well", "Ask for a bandage" and "Offer to help restock". Class teachers get "Ask about today's lesson" and "Offer to carry her papers".
- Every line now knows who may say it: a teacher no longer says a student's line, and the nurse no longer talks about classes, grades or lesson plans. Introductions and first meetings follow her job too.
- "Admit about the others" only shows for a girl who is already interested in you.
- A girl the director brings to your map now really appears there, instead of staying out of sight.

## 3.3.0
- Boys in the class: the male folder shows next to female in the class roster and a male card takes a seat. He uses the boys' toilet, loses reputation in the girls' areas and catches girls who walk into the boys' toilet. "Likes girls" counts the same for everyone.
- Teachers stay teachers. A teacher whose event is over only takes a student seat in the code: she walks and talks freely, keeps a teacher's lines and is "-sensei" to everyone. Her mood comes from her job (a raise, her class's results, praise from the principal, grading all night, lesson plans, the staff party, parents, a complaint, a staff meeting), the same reason all day, and you can ask about it. Students react to you and a teacher ("You did it with the teacher?!"), teachers scold, gossip from the staff room, and in a talk with both a teacher and a student, or two colleagues, speak to each other as what they are.
- Touching her hand, chest and butt in a talk goes through the director with the four answers, like the kiss, with wary lines for each.
- Options: one difficulty multiplier per pace. An open friend gives a wary yes on day 3 and a yes on day 4 for touches and the kiss, and a yes to H on day 4.
- Talk: answers now reply to what she just said, including a like she lets slip at the end of her line. Small talk options follow the shape of her line: a statement, a question, an offer, or "want to see / hear / try some?".
- Talk with two girls: both stand side by side from the first line, and what they say to each other is written as line and answer, about things that are true right now (who they are, the hour, the rain, exams, their club, what they share).
- The second girl no longer slides away during a talk with two.
- The portrait of a teacher or graduate who joined school life no longer shows "?".
- Fixed a load that could hang forever on "Going to: ..." the day after a teacher or graduate took a seat, when KK_SkinEffects read her missing AI entry.

## 3.2.0
- Male cards: a card saved as male is detected on its own. Lines about him say he/him/his and boyfriend, his own lines call himself a boyfriend or a guy, his card says "Boyfriend"; voice and personality stay the same.
- A girlfriend is never below a friend with the same favor when you ask to hold hands, for a date or for a kiss (an upset girlfriend with low trust used to refuse your hand forever while a friend who liked you would take it).
- Three more versions of every line in every voice (4,464 lines now have four or more per voice).
- Plugin options reviewed: sections are numbered 01 to 10 so the Configuration Manager lists them in order (old values carry over); "Achievement pop-ups" turns the Steam-style card off; "Debug tools" and "Enable Chat button" (read by nothing) are gone; "How are you today" and "Conversation by scene" say what they really do.

## 3.1.0
- A wary no is now an almost-yes, the mirror of the wary yes: she wants it (her bars, what already happened between you, a flirty day, trust), and something else held her back. She says what: her personality, her mood, guilt about last time, your girlfriend, too soon, you already asked today, a grudge, feeling neglected, your reputation, or just not here and now.

## 3.0.0
- Nothing in a talk acts while a line is still on screen: categories, options, the target button and touches wait until the box closes (a click during a line is ignored, with a short beat after it closes).
- H no longer needs dating, for every girl: the director reads the context (what you are to her, her pace and bars, the days together, your reputation, trust, mood, H before, a recent kiss, another girlfriend, a perfect day, how often she already said no) and answers yes, a wary yes, a wary no ("not now") or no. A reserved girl who is not dating you needs very high bars; a normal one less; an open one mostly her lewdness.
- After a wary yes she feels guilty: next talk she asks you to take responsibility or pulls away, and you settle it (ask her out, say it meant something, ask for time, give her space, keep it secret, or call it a one-time thing). Until then her H, kisses and invitations come harder.
- Your girlfriend finding out about another girl is the director's call too: her mood, anger, trust, how often it happened, that girl (friend or rival), how far it went and whether she saw it. Explaining yourself reads her mood.
- Her guilt scene opens the next talk before any other subject.
- Balance after the tests: a girl who does not know you never says yes to H; a reserved friend can, with maxed bars and a good context; "not now" comes from interest added up (a grudge lowers it instead of ruling it out); with many girlfriends, the count of past flirts weighs less, so a girlfriend in a good mood can tease instead of exploding.
- The trio's "To: ..." caption no longer runs past the screen edge.

### Also in 3.0.0 (2026-10-03)
- Achievements climb through stages, each with its own name and goal; each story heroine and teacher has a climb with one stage per part of her story. Old saves keep their records (moved to the matching stage).
- Before the game's H, a girl says what her card refuses (protection, kisses, the back door, touching, toys), in her own voice; the game's H is unchanged. Touching her lips or body also answers with her card's refusals.
- Talking with two girls: options that only make sense in a pair (ask about the other, an embarrassing story, does she talk about you, a note, invite both, a compliment in front of her, a hidden touch), with the other's reaction.
- Talk with two girls: the target button sits under Spicy at 80% size, right-aligned; the top icons in one row; Talk is blue and the target white.
- Affection and H fill slowly: smaller talk rewards and a daily curve for both (whole, then half, then a tenth), wider for open girls and narrower for reserved ones. No more minimum number of days: a well-played normal girl dates you on day ~4 and says yes to H on day ~5, a reserved one ~5 and ~7, an open one 2-3 and H on a perfect first day. One pace profile for answers and gains (they disagreed).
- Clicking the second girl's body in a talk with both works with the real mouse (the player's own sight triggers were in the way).
- More dialogue lines (touch, pair talk, refusals).

## 2.x milestones
- **2.9 (2026-10-03)**: three-way talk stage, Steam-style achievement cards, mod detection, card traits.
- **2.8 (2026-09-29)**: talk with two girls (all phases), the director's wary kiss, the achievements Album.
- **2.7 (2026-09-29)**: rain, witnesses, confiding, clearing the air, giving back a spot.
- **2.6 (2026-09-25)**: precise talk, jealousy by relationship, shared girlfriends.
- **2.5 (2026-09-23)**: varied short answers, telling her gossip, the girls' own reputation and bonds.
- **2.4 / 2.4.1 (2026-09-22)**: procedural talk topics, lore, the dialogue bank, status pages; gossip spread fix, the rooftop note scene, the director in charge of first actions.
- **2.3 (2026-09-22)**.
- **2.0.0 (2026-09-04)**: the school life director (needs, pairs and couples, molds, redemption, harem), conversation as scenes, season events (Sundays, relationship chapters, rumors, the club project, the council election).

## 1.x
- **1.9.0 (2026-09-04)**, **1.7.0** spotlight episodes staged like story mode, **1.6.0**, **1.5.0 (2026-09-03)**.
- **1.0.0 (2026-08-25)**: first version: club board, moods, gifts and store, activities and jobs.
