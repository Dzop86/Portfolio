--  War, the card game. Two players split a shuffled deck; each ply both show their top card and the
--  higher takes both. Equal cards start a war: each adds a face-down and a face-up card, and the
--  higher face-up card takes them all (wars can chain). A player who cannot go on loses; a game that
--  reaches Max_Plies is declared endless (with a fixed way of picking up cards, War can cycle).
with Interfaces;
with Cards; use Cards;

package War is

   type Player is (First, Second);
   type Ending is (Won, Draw, Endless);

   type Result is record
      Ending  : War.Ending;
      Winner  : Player;      --  meaningful when Ending = Won
      Plies   : Natural;     --  plies played
      Wars    : Natural;     --  wars started (a chained war counts once per tie)
   end record;

   type Hands is array (Player) of Pile;

   --  The deck shuffled from a seed, the same on every system (xorshift64, Fisher-Yates).
   function Shuffled (Seed : Interfaces.Unsigned_64) return Pile
     with Post => Length (Shuffled'Result) = Deck_Size;

   --  Deals a deck one card each in turn: 26 cards per player for a full deck.
   function Deal (Deck : Pile) return Hands
     with Post => Length (Deal'Result (First)) + Length (Deal'Result (Second)) = Length (Deck);


   --  Plays a game from the given hands. The cards of both hands are kept: at every ply their total
   --  stays what it was at the start (checked by an assertion).
   --  Watch, if given, is called after each ply with the cards shown face up and who took the trick (an
   --  anonymous access parameter, so a procedure declared anywhere can be passed).
   function Play
     (Start     : Hands;
      Max_Plies : Positive := 10_000;
      Watch     : access procedure (Ply : Positive; Shown : String; Taker : Player) := null) return Result
     with Pre => Length (Start (First)) + Length (Start (Second)) <= Deck_Size;

   --  A whole game from a seed.
   function Play_Seed
     (Seed      : Interfaces.Unsigned_64;
      Max_Plies : Positive := 10_000;
      Watch     : access procedure (Ply : Positive; Shown : String; Taker : Player) := null) return Result
   is (Play (Deal (Shuffled (Seed)), Max_Plies, Watch));

end War;
