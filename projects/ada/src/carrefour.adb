--  Command-line simulation: prints the lights second by second.
--    carrefour [SECONDS] [EW_REQUEST_TIME]
--    carrefour --automaton   (every reachable state as JSON, for the project page)
with Ada.Command_Line; use Ada.Command_Line;
with Ada.Text_IO;      use Ada.Text_IO;
with Automaton;
with Traffic;          use Traffic;

procedure Carrefour is
   Symbol : constant array (Color) of Character := [Red => 'R', Yellow => 'Y', Green => 'G'];
   Timings : constant Timing := (NS_Green => 30, EW_Green => 20);
   C : Controller := Start (Timings);
   Length  : Natural := 70;
   Request : Integer := -1;
begin
   if Argument_Count = 1 and then Argument (1) = "--automaton" then
      Automaton.Put_Json (Timings);
      return;
   end if;
   if Argument_Count >= 1 then
      Length := Natural'Value (Argument (1));
   end if;
   if Argument_Count >= 2 then
      Request := Integer'Value (Argument (2));
   end if;
   Put_Line ("  t  NS EW  phase");
   for T in 0 .. Length loop
      if T = Request then
         Request_Crossing (C, East_West);
         Put_Line ("     -- east-west request --");
      end if;
      Put_Line ((if T < 10 then "  " elsif T < 100 then " " else "") & Natural'Image (T)
                & "  " & Symbol (Light (Current (C), North_South)) & "  " & Symbol (Light (Current (C), East_West))
                & "  " & Phase'Image (Current (C)));
      Tick (C);
   end loop;
exception
   when Constraint_Error =>
      Put_Line (Standard_Error, "usage: carrefour [SECONDS] [EW_REQUEST_TIME]");
      Set_Exit_Status (Failure);
end Carrefour;
