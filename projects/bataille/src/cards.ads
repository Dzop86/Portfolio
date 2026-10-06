--  Cards and piles. A card's rank is a range type and its suit an enumeration: a "1 of hearts" or a
--  fifth suit cannot be written. A pile is a circular queue of at most 52 cards, whose length every
--  operation states in its contract.
with Ada.Strings.UTF_Encoding;

package Cards is

   type Suit is (Clubs, Diamonds, Hearts, Spades);
   type Rank is range 2 .. 14;  --  11 jack, 12 queen, 13 king, 14 ace

   type Card is record
      Value  : Rank;
      Colour : Suit;
   end record;

   Deck_Size : constant := 52;
   subtype Count is Natural range 0 .. Deck_Size;

   type Pile is private;

   function Length (P : Pile) return Count;
   function Is_Empty (P : Pile) return Boolean is (Length (P) = 0);

   --  The card that would be played next.
   function Top (P : Pile) return Card
     with Pre => not Is_Empty (P);

   --  Takes the top card.
   procedure Take_Top (P : in out Pile; C : out Card)
     with Pre  => not Is_Empty (P),
          Post => Length (P) = Length (P'Old) - 1 and then C = Top (P'Old);

   --  Puts a card under the pile.
   procedure Put_Bottom (P : in out Pile; C : Card)
     with Pre  => Length (P) < Deck_Size,
          Post => Length (P) = Length (P'Old) + 1;

   --  The 52 cards in order (clubs 2 to ace, then diamonds, hearts, spades).
   function New_Deck return Pile
     with Post => Length (New_Deck'Result) = Deck_Size;

   --  "Q", "10", "A"... then the suit symbol, as UTF-8 bytes (write them with Put_UTF_8, not Put_Line,
   --  which would encode them a second time).
   function Image (C : Card) return Ada.Strings.UTF_Encoding.UTF_8_String;

private

   type Slots is array (0 .. Deck_Size - 1) of Card;

   type Pile is record
      Cards : Slots := [others => (2, Clubs)];
      First : Natural range 0 .. Deck_Size - 1 := 0;
      Size  : Count := 0;
   end record;

   function Length (P : Pile) return Count is (P.Size);

end Cards;
