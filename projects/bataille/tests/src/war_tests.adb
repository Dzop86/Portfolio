with Ada.Assertions;
with AUnit.Assertions; use AUnit.Assertions;
with Cards;            use Cards;
with Interfaces;       use type Interfaces.Unsigned_64;
with Stats;
with War;              use War;

package body War_Tests is

   use AUnit.Test_Cases.Registration;

   type Card_List is array (Positive range <>) of Card;

   function Make (List : Card_List) return Pile is
      P : Pile;
   begin
      for C of List loop
         Put_Bottom (P, C);
      end loop;
      return P;
   end Make;

   function Same_Cards (A, B : Pile) return Boolean is
      X : Pile := A;
      Y : Pile := B;
      C, D : Card;
   begin
      if Length (X) /= Length (Y) then
         return False;
      end if;
      while not Is_Empty (X) loop
         Take_Top (X, C);
         Take_Top (Y, D);
         if C /= D then
            return False;
         end if;
      end loop;
      return True;
   end Same_Cards;

   --  The 52 cards of a pile are all different.
   function All_Different (P : Pile) return Boolean is
      Seen : array (Rank, Suit) of Boolean := [others => [others => False]];
      X    : Pile := P;
      C    : Card;
   begin
      while not Is_Empty (X) loop
         Take_Top (X, C);
         if Seen (C.Value, C.Colour) then
            return False;
         end if;
         Seen (C.Value, C.Colour) := True;
      end loop;
      return True;
   end All_Different;

   procedure Test_Deck_And_Piles (T : in out AUnit.Test_Cases.Test_Case'Class) is
      pragma Unreferenced (T);
      P : Pile := Make ([(2, Clubs), (14, Spades), (10, Hearts)]);
      C : Card;
   begin
      Assert (Length (New_Deck) = 52 and All_Different (New_Deck), "a deck has 52 different cards");
      Assert (Top (P) = (2, Clubs), "first card on top");
      Take_Top (P, C);
      Put_Bottom (P, C);
      Assert (Length (P) = 3 and Top (P) = (14, Spades), "taken from the top, put under");
      Take_Top (P, C);
      Take_Top (P, C);
      Take_Top (P, C);
      Assert (C = (2, Clubs) and Is_Empty (P), "first in, first out");
      begin
         Take_Top (P, C);
         Assert (False, "taking from an empty pile must break the precondition");
      exception
         when Ada.Assertions.Assertion_Error => null;
      end;
      Assert (Image ((14, Spades)) = "A" & Character'Val (16#E2#) & Character'Val (16#99#) & Character'Val (16#A0#),
              "A of spades in UTF-8");
      Assert (Image ((10, Hearts))'Length = 2 + 3 and Image ((12, Diamonds))(1) = 'Q', "10 and Q");
   end Test_Deck_And_Piles;

   procedure Test_Shuffle_And_Deal (T : in out AUnit.Test_Cases.Test_Case'Class) is
      pragma Unreferenced (T);
      D : constant Pile := Shuffled (42);
      H : constant Hands := Deal (Make ([(2, Clubs), (3, Clubs), (4, Clubs), (5, Clubs)]));
   begin
      Assert (All_Different (D), "a shuffle is a permutation");
      Assert (Same_Cards (D, Shuffled (42)), "the same seed gives the same deck");
      Assert (not Same_Cards (D, Shuffled (43)), "another seed gives another deck");
      Assert (not Same_Cards (D, New_Deck), "the deck is actually shuffled");
      Assert (Same_Cards (H (First), Make ([(2, Clubs), (4, Clubs)])), "dealt one card each in turn");
      Assert (Same_Cards (H (Second), Make ([(3, Clubs), (5, Clubs)])), "second hand");
      Assert (Length (Deal (Shuffled (7)) (First)) = 26, "26 cards each");
   end Test_Shuffle_And_Deal;

   procedure Test_A_Ply_And_A_War (T : in out AUnit.Test_Cases.Test_Case'Class) is
      pragma Unreferenced (T);
      R : Result;
   begin
      R := Play ([First => Make ([(14, Spades)]), Second => Make ([(2, Clubs)])]);
      Assert (R = (Won, First, 1, 0), "the ace takes the two");
      --  5 against 5: a war; face down 2 and 3, face up king against queen: the first takes all six.
      R := Play ([First  => Make ([(5, Spades), (2, Diamonds), (13, Hearts)]),
                  Second => Make ([(5, Hearts), (3, Clubs), (12, Diamonds)])]);
      Assert (R = (Won, First, 1, 1), "the war goes to the king");
      --  Wars can chain: 5-5, then 9-9, then 4 against jack.
      R := Play ([First  => Make ([(5, Spades), (2, Diamonds), (9, Hearts), (3, Spades), (4, Clubs)]),
                  Second => Make ([(5, Hearts), (3, Clubs), (9, Diamonds), (6, Spades), (11, Clubs)])]);
      Assert (R = (Won, Second, 1, 2), "a double war goes to the jack");
   end Test_A_Ply_And_A_War;

   procedure Test_Ends_Of_Game (T : in out AUnit.Test_Cases.Test_Case'Class) is
      pragma Unreferenced (T);
      R : Result;
   begin
      --  A war the first player cannot pay for (one card left): the second wins.
      R := Play ([First => Make ([(5, Spades)]), Second => Make ([(5, Hearts), (3, Clubs), (4, Diamonds)])]);
      Assert (R.Ending = Won and R.Winner = Second, "out of cards during a war");
      R := Play ([First => Make ([(5, Spades)]), Second => Make ([(5, Hearts)])]);
      Assert (R.Ending = Draw, "neither can pay for the war");
      --  Seed 1 lasts 2 870 plies: stopped at 10, it is reported endless after exactly 10 plies.
      R := Play_Seed (1, Max_Plies => 10);
      Assert (R.Ending = Endless and R.Plies = 10, "a game cut at the limit is endless");
      R := Play_Seed (1);
      Assert (R = (Won, Second, 2870, 55), "seed 1, the same on every system");
   end Test_Ends_Of_Game;

   Calls : Natural := 0;
   procedure Count_Call (Ply : Positive; Shown : String; Taker : Player) is
      pragma Unreferenced (Shown, Taker);
   begin
      Calls := Calls + 1;
      Assert (Ply = Calls, "plies are reported in order");
   end Count_Call;

   procedure Test_Many_Games (T : in out AUnit.Test_Cases.Test_Case'Class) is
      pragma Unreferenced (T);
      R : Result;
   begin
      --  200 games: each ends (or is cut at 10 000 plies), and Play's assertion has checked at every
      --  ply that no card appeared or vanished.
      for Seed in Interfaces.Unsigned_64 range 1 .. 200 loop
         R := Play_Seed (Seed);
         Assert (R.Plies > 0 and R.Plies <= 10_000, "plies within the limit");
         Assert (R.Ending /= Endless or R.Plies = 10_000, "endless means the limit was reached");
         Assert (R = Play_Seed (Seed), "deterministic");
      end loop;
      Calls := 0;
      R := Play_Seed (3, Max_Plies => 50, Watch => Count_Call'Access);
      Assert (Calls = R.Plies, "one report per ply");
   end Test_Many_Games;

   procedure Test_Statistics (T : in out AUnit.Test_Cases.Test_Case'Class) is
      pragma Unreferenced (T);
      S : constant Stats.Summary := Stats.Run (200);
   begin
      Assert (S.First_Wins + S.Second_Wins + S.Draws + S.Endless = 200, "every game counted once");
      Assert (S.Shortest <= S.Plies_Max, "shortest no longer than longest");
      Assert (Stats.Hundredths (1234) = "12.34" and Stats.Hundredths (5) = "0.05" and Stats.Hundredths (100) = "1.00",
              "hundredths");
      --  Totals of 100 000 long games times 100 do not fit in 32 bits (it overflowed once).
      Assert (Stats.Hundredths (9_000_000_000_00) = "9000000000.00", "large totals");
   end Test_Statistics;

   overriding procedure Register_Tests (T : in out Test) is
   begin
      Register_Routine (T, Test_Deck_And_Piles'Access, "deck and piles");
      Register_Routine (T, Test_Shuffle_And_Deal'Access, "shuffle and deal");
      Register_Routine (T, Test_A_Ply_And_A_War'Access, "a ply and a war");
      Register_Routine (T, Test_Ends_Of_Game'Access, "ends of game");
      Register_Routine (T, Test_Many_Games'Access, "many games");
      Register_Routine (T, Test_Statistics'Access, "statistics");
   end Register_Tests;

   overriding function Name (T : Test) return AUnit.Message_String is
      pragma Unreferenced (T);
   begin
      return AUnit.Format ("War");
   end Name;

end War_Tests;
