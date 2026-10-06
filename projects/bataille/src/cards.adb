package body Cards is

   function Top (P : Pile) return Card is (P.Cards (P.First));

   procedure Take_Top (P : in out Pile; C : out Card) is
   begin
      C := P.Cards (P.First);
      P.First := (P.First + 1) mod Deck_Size;
      P.Size := P.Size - 1;
   end Take_Top;

   procedure Put_Bottom (P : in out Pile; C : Card) is
   begin
      P.Cards ((P.First + P.Size) mod Deck_Size) := C;
      P.Size := P.Size + 1;
   end Put_Bottom;

   function New_Deck return Pile is
      Result : Pile;
   begin
      for S in Suit loop
         for R in Rank loop
            Put_Bottom (Result, (R, S));
         end loop;
      end loop;
      return Result;
   end New_Deck;

   --  UTF-8 bytes of the suit symbols, written as bytes so that the source encoding does not matter.
   E2 : constant Character := Character'Val (16#E2#);
   S99 : constant Character := Character'Val (16#99#);

   function Image (C : Card) return Ada.Strings.UTF_Encoding.UTF_8_String is
      Value : constant String :=
        (case C.Value is
            when 11 => "J",
            when 12 => "Q",
            when 13 => "K",
            when 14 => "A",
            when 10 => "10",
            when others => [1 => Character'Val (Character'Pos ('0') + Integer (C.Value))]);
      Symbol : constant String :=
        (case C.Colour is
            when Clubs    => E2 & S99 & Character'Val (16#A3#),
            when Diamonds => E2 & S99 & Character'Val (16#A6#),
            when Hearts   => E2 & S99 & Character'Val (16#A5#),
            when Spades   => E2 & S99 & Character'Val (16#A0#));
   begin
      return Value & Symbol;
   end Image;

end Cards;
