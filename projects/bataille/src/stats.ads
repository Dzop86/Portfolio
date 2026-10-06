--  Statistics of many games (seeds 1 to N), printed as JSON for the project page. Averages are computed
--  in integers (hundredths), so the output is the same on every system.
package Stats is

   --  Totals over many games: 100 000 games of ~1 600 plies, times 100 for the hundredths, overflow 32 bits.
   type Total is range 0 .. 2 ** 62;

   type Summary is record
      Games        : Natural := 0;
      First_Wins   : Natural := 0;
      Second_Wins  : Natural := 0;
      Draws        : Natural := 0;
      Endless      : Natural := 0;
      Plies_Total  : Total := 0;    --  over the games that ended
      Plies_Max    : Natural := 0;
      Longest_Seed : Natural := 0;
      Wars_Total   : Total := 0;    --  over the games that ended
      Shortest     : Natural := Natural'Last;
   end record;

   function Run (Games : Positive; Max_Plies : Positive := 10_000) return Summary;

   --  "12.34" for 1234 hundredths.
   function Hundredths (Value_Times_100 : Total) return String;

   function To_Json (S : Summary) return String;

end Stats;
