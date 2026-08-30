using System.Collections.Generic;
using System.Linq;

namespace Joojkk.CampusLife
{
    internal enum ItemType { Gift, Consumable, Key }

    internal sealed class ItemDef
    {
        public string Id;
        public string Name;
        public string Desc;
        public ItemType Type;
        public int Price;
        public string[] Likes = new string[0];  // profile likes it matches (GirlInfo.LikeIds)
        public int BaseFavor = 3;               // favor even if she doesn't care
        public int LikedFavor = 10;             // favor when it matches
        public int Lewd;                        // lewdness change
        public bool Intimate;                   // only appropriate for girlfriends / very lewd girls
        public bool LunchOnly;                  // can only be bought at lunch (fresh food)
    }

    internal static class Items
    {
        public static readonly List<ItemDef> All = new List<ItemDef>
        {
            // --- gifts ---
            new ItemDef { Id = "chocolate", Name = "Chocolate bar", Desc = "Safe choice. Everybody likes chocolate a little.", Type = ItemType.Gift, Price = 350, Likes = new[] { "sweets" }, BaseFavor = 4, LikedFavor = 9 },
            new ItemDef { Id = "cookies", Name = "Bakery cookies", Desc = "A cute box of butter cookies.", Type = ItemType.Gift, Price = 600, Likes = new[] { "sweets" }, BaseFavor = 3, LikedFavor = 12 },
            new ItemDef { Id = "daifuku", Name = "Strawberry daifuku", Desc = "Seasonal sweet from the station shop.", Type = ItemType.Gift, Price = 450, Likes = new[] { "sweets", "food" }, BaseFavor = 3, LikedFavor = 11 },
            new ItemDef { Id = "canned_coffee", Name = "Black canned coffee", Desc = "Bitter. Only some girls appreciate it.", Type = ItemType.Gift, Price = 150, Likes = new[] { "coffee" }, BaseFavor = 1, LikedFavor = 8 },
            new ItemDef { Id = "coffee_beans", Name = "Premium coffee beans", Desc = "For the real coffee lover.", Type = ItemType.Gift, Price = 1100, Likes = new[] { "coffee" }, BaseFavor = 2, LikedFavor = 15 },
            new ItemDef { Id = "spicy_chips", Name = "Ghost-pepper chips", Desc = "Dangerously hot.", Type = ItemType.Gift, Price = 300, Likes = new[] { "spicy" }, BaseFavor = 0, LikedFavor = 11 },
            new ItemDef { Id = "bento", Name = "Deluxe bento", Desc = "A filling lunch box. Best at lunch time.", Type = ItemType.Gift, Price = 800, Likes = new[] { "food" }, BaseFavor = 4, LikedFavor = 12, LunchOnly = true },
            new ItemDef { Id = "cookbook", Name = "Recipe book", Desc = "Bento recipes for beginners.", Type = ItemType.Gift, Price = 1200, Likes = new[] { "cooking" }, BaseFavor = 2, LikedFavor = 14 },
            new ItemDef { Id = "apron", Name = "Frilly apron", Desc = "Cute and practical.", Type = ItemType.Gift, Price = 1500, Likes = new[] { "cooking", "fashion" }, BaseFavor = 3, LikedFavor = 14, Lewd = 2 },
            new ItemDef { Id = "sports_drink", Name = "Sports drink", Desc = "Cold, from the vending machine.", Type = ItemType.Gift, Price = 200, Likes = new[] { "exercise" }, BaseFavor = 2, LikedFavor = 8 },
            new ItemDef { Id = "wristband", Name = "Team wristband", Desc = "For athletes.", Type = ItemType.Gift, Price = 700, Likes = new[] { "exercise" }, BaseFavor = 2, LikedFavor = 12 },
            new ItemDef { Id = "notebook", Name = "Fancy notebook set", Desc = "Pastel notebooks and sticky notes.", Type = ItemType.Gift, Price = 500, Likes = new[] { "study" }, BaseFavor = 3, LikedFavor = 10 },
            new ItemDef { Id = "pen", Name = "Fountain pen", Desc = "Elegant and a bit expensive.", Type = ItemType.Gift, Price = 1400, Likes = new[] { "study" }, BaseFavor = 3, LikedFavor = 15 },
            new ItemDef { Id = "hair_clip", Name = "Hair clip", Desc = "A small ribbon clip.", Type = ItemType.Gift, Price = 800, Likes = new[] { "fashion" }, BaseFavor = 4, LikedFavor = 12 },
            new ItemDef { Id = "magazine", Name = "Fashion magazine", Desc = "This month's trends.", Type = ItemType.Gift, Price = 450, Likes = new[] { "fashion" }, BaseFavor = 2, LikedFavor = 9 },
            new ItemDef { Id = "plush_cat", Name = "Cat plushie", Desc = "Soft and round.", Type = ItemType.Gift, Price = 1100, Likes = new[] { "animals" }, BaseFavor = 4, LikedFavor = 14 },
            new ItemDef { Id = "photo_book", Name = "Kitten photo book", Desc = "Two hundred pages of kittens.", Type = ItemType.Gift, Price = 650, Likes = new[] { "animals" }, BaseFavor = 2, LikedFavor = 11 },
            new ItemDef { Id = "flowers", Name = "Small bouquet", Desc = "Romantic. Works best on girls who already like you.", Type = ItemType.Gift, Price = 1000, BaseFavor = 7, LikedFavor = 7, Lewd = 1 },
            new ItemDef { Id = "perfume", Name = "Perfume", Desc = "A sweet, grown-up scent.", Type = ItemType.Gift, Price = 2500, Likes = new[] { "fashion" }, BaseFavor = 5, LikedFavor = 16, Lewd = 5 },
            new ItemDef { Id = "lingerie", Name = "Lace lingerie", Desc = "Bold. Only for a girlfriend or a very open girl.", Type = ItemType.Gift, Price = 3000, BaseFavor = 6, LikedFavor = 6, Lewd = 18, Intimate = true },
            new ItemDef { Id = "toy", Name = "'Massager'", Desc = "Very bold. Only for a girlfriend or a very open girl.", Type = ItemType.Gift, Price = 3500, BaseFavor = 4, LikedFavor = 4, Lewd = 25, Intimate = true },

            // --- consumables ---
            new ItemDef { Id = "energy_drink", Name = "Energy drink", Desc = "Restores 2 energy. Use from the planner.", Type = ItemType.Consumable, Price = 400 },
            new ItemDef { Id = "study_guide", Name = "Study guide", Desc = "Your next study activity gives double Intelligence.", Type = ItemType.Consumable, Price = 900 },
            new ItemDef { Id = "protein", Name = "Protein shake", Desc = "Your next workout gives double Strength.", Type = ItemType.Consumable, Price = 900 },
            new ItemDef { Id = "cologne", Name = "Cologne", Desc = "For the rest of the day, conversations give +2 favor.", Type = ItemType.Consumable, Price = 1200 },
            new ItemDef { Id = "omamori", Name = "Lucky charm", Desc = "+15 on your next stat check (exams, festivals, happenings).", Type = ItemType.Consumable, Price = 800 },
            new ItemDef { Id = "cat_food", Name = "Cat food", Desc = "For the stray cats in the courtyard.", Type = ItemType.Consumable, Price = 200 },
        };

        public static ItemDef Get(string id) { return All.FirstOrDefault(i => i.Id == id); }

        public static string NameOf(string id)
        {
            var i = Get(id);
            return i != null ? i.Name : id;
        }

        public static int PriceOf(ItemDef i)
        {
            return System.Math.Max(10, (int)(i.Price * Cfg.PriceMultiplier.Value));
        }

        /// <summary>Gift categories used by board requests ("bring her something sweet").</summary>
        public static bool MatchesCategory(ItemDef item, string likeId)
        {
            return item != null && item.Type == ItemType.Gift && item.Likes.Contains(likeId);
        }
    }
}
