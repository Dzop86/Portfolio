with Ada.Strings.Unbounded; use Ada.Strings.Unbounded;

package body War is

   use type Interfaces.Unsigned_64;

   function Shuffled (Seed : Interfaces.Unsigned_64) return Pile is
      type Deck_Array is array (1 .. Deck_Size) of Card;
      Cards_In : Deck_Array;
      Source   : Pile := New_Deck;
      State    : Interfaces.Unsigned_64 := (if Seed = 0 then 16#9E37_79B9_7F4A_7C15# else Seed);
      Result   : Pile;

      function Next return Interfaces.Unsigned_64 is
      begin
         State := State xor Interfaces.Shift_Left (State, 13);
         State := State xor Interfaces.Shift_Right (State, 7);
         State := State xor Interfaces.Shift_Left (State, 17);
         return State;
      end Next;
   begin
      for I in Cards_In'Range loop
         Take_Top (Source, Cards_In (I));
      end loop;
      for I in reverse 2 .. Deck_Size loop
         declare
            J   : constant Positive := Natural (Next mod Interfaces.Unsigned_64 (I)) + 1;  --  1 .. I
            Tmp : constant Card := Cards_In (I);
         begin
            Cards_In (I) := Cards_In (J);
            Cards_In (J) := Tmp;
         end;
      end loop;
      for C of Cards_In loop
         Put_Bottom (Result, C);
      end loop;
      return Result;
   end Shuffled;

   function Deal (Deck : Pile) return Hands is
      Rest   : Pile := Deck;
      Result : Hands;
      Turn   : Player := First;
      C      : Card;
   begin
      while not Is_Empty (Rest) loop
         Take_Top (Rest, C);
         Put_Bottom (Result (Turn), C);
         Turn := (if Turn = First then Second else First);
      end loop;
      return Result;
   end Deal;

   function Play
     (Start     : Hands;
      Max_Plies : Positive := 10_000;
      Watch     : access procedure (Ply : Positive; Shown : String; Taker : Player) := null) return Result is
      H     : Hands := Start;
      Total : constant Cards.Count := Length (Start (First)) + Length (Start (Second));
      Table : Pile;  --  cards in play during the current ply, in the order they were put down
      Plies : Natural := 0;
      Wars  : Natural := 0;
      Up    : array (Player) of Card;
      Shown : Unbounded_String;
      C     : Card;

      procedure Show (P : Player) is
      begin
         Take_Top (H (P), Up (P));
         Put_Bottom (Table, Up (P));
      end Show;

      function Loser_When_Out return Result is
        (if Is_Empty (H (First)) and Is_Empty (H (Second)) then (Draw, First, Plies, Wars)
         elsif Is_Empty (H (First)) then (Won, Second, Plies, Wars)
         else (Won, First, Plies, Wars));
   begin
      loop
         if Is_Empty (H (First)) or Is_Empty (H (Second)) then
            return Loser_When_Out;
         end if;
         if Plies = Max_Plies then
            return (Endless, First, Plies, Wars);
         end if;
         Plies := Plies + 1;
         Show (First);
         Show (Second);
         Shown := To_Unbounded_String (Image (Up (First)) & " / " & Image (Up (Second)));
         while Up (First).Value = Up (Second).Value loop
            Wars := Wars + 1;
            --  Each player needs a face-down and a face-up card; the one who cannot loses the game.
            if Length (H (First)) < 2 or Length (H (Second)) < 2 then
               if Length (H (First)) < 2 and Length (H (Second)) < 2 then
                  return (Draw, First, Plies, Wars);
               end if;
               return (Won, (if Length (H (First)) < 2 then Second else First), Plies, Wars);
            end if;
            for P in Player loop
               Take_Top (H (P), C);
               Put_Bottom (Table, C);  --  face down
            end loop;
            Show (First);
            Show (Second);
            Append (Shown, ", " & Image (Up (First)) & " / " & Image (Up (Second)));
         end loop;
         declare
            Taker : constant Player := (if Up (First).Value > Up (Second).Value then First else Second);
         begin
            while not Is_Empty (Table) loop
               Take_Top (Table, C);
               Put_Bottom (H (Taker), C);
            end loop;
            pragma Assert (Length (H (First)) + Length (H (Second)) = Total, "cards appeared or vanished");
            if Watch /= null then
               Watch (Plies, To_String (Shown), Taker);
            end if;
         end;
      end loop;
   end Play;

end War;
