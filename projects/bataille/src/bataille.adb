--  War in the terminal.
--    bataille              a game shuffled at random, ply by ply
--    bataille --seed N     the game of seed N (the same on every system)
--    bataille --stats N    statistics of the games of seeds 1 to N, as JSON
with Ada.Calendar;
with Ada.Command_Line; use Ada.Command_Line;
with Ada.Text_IO;      use Ada.Text_IO;
with Ada.Text_IO.Text_Streams;
with Interfaces;
with Stats;
with War;              use War;

procedure Bataille is

   use type Interfaces.Unsigned_64;

   procedure Usage is
   begin
      Put_Line (Standard_Error, "usage: bataille [--seed N | --stats N]");
      Set_Exit_Status (2);
   end Usage;

   --  Writes UTF-8 bytes as they are: Put_Line would encode them again (the compiler reads sources as
   --  UTF-8, so Text_IO encodes every character above 127).
   procedure Put_UTF_8_Line (S : String) is
   begin
      String'Write (Ada.Text_IO.Text_Streams.Stream (Current_Output), S & Character'Val (10));
   end Put_UTF_8_Line;

   procedure Print_Ply (Ply : Positive; Shown : String; Taker : Player) is
   begin
      Put_UTF_8_Line ("Ply" & Ply'Image & ": " & Shown & " -> " & (if Taker = First then "first" else "second"));
   end Print_Ply;

   procedure Play_And_Print (Seed : Interfaces.Unsigned_64) is
      R : constant Result := Play_Seed (Seed, 10_000, Print_Ply'Access);
   begin
      Put_Line ("Seed" & Seed'Image & ":"
        & (case R.Ending is
             when Won     => " the " & (if R.Winner = First then "first" else "second") & " player wins",
             when Draw    => " draw",
             when Endless => " endless game")
        & " after" & R.Plies'Image & " plies and" & R.Wars'Image & " wars.");
   end Play_And_Print;

   --  The number after --seed or --stats; Constraint_Error (caught below) only for a malformed number,
   --  so that an error in the game itself is not mistaken for a usage error.
   Number : Natural := 0;
begin
   if Argument_Count = 2 then
      begin
         Number := Natural'Value (Argument (2));
      exception
         when Constraint_Error =>
            Usage;
            return;
      end;
   end if;
   if Argument_Count = 0 then
      Play_And_Print (Interfaces.Unsigned_64 (Ada.Calendar.Seconds (Ada.Calendar.Clock) * 1000.0) + 1);
   elsif Argument_Count = 2 and then Argument (1) = "--seed" then
      Play_And_Print (Interfaces.Unsigned_64 (Number));
   elsif Argument_Count = 2 and then Argument (1) = "--stats" and then Number > 0 then
      Put_Line (Stats.To_Json (Stats.Run (Number)));
   else
      Usage;
   end if;
end Bataille;
