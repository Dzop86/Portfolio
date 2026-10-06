with Interfaces;
with War;

package body Stats is

   use War;

   function Run (Games : Positive; Max_Plies : Positive := 10_000) return Summary is
      S : Summary;
   begin
      for Seed in 1 .. Games loop
         declare
            R : constant Result := Play_Seed (Interfaces.Unsigned_64 (Seed), Max_Plies);
         begin
            S.Games := S.Games + 1;
            case R.Ending is
               when Endless =>
                  S.Endless := S.Endless + 1;
               when Draw | Won =>
                  if R.Ending = Draw then
                     S.Draws := S.Draws + 1;
                  elsif R.Winner = First then
                     S.First_Wins := S.First_Wins + 1;
                  else
                     S.Second_Wins := S.Second_Wins + 1;
                  end if;
                  S.Plies_Total := S.Plies_Total + Total (R.Plies);
                  S.Wars_Total := S.Wars_Total + Total (R.Wars);
                  S.Shortest := Natural'Min (S.Shortest, R.Plies);
                  if R.Plies > S.Plies_Max then
                     S.Plies_Max := R.Plies;
                     S.Longest_Seed := Seed;
                  end if;
            end case;
         end;
      end loop;
      return S;
   end Run;

   function Trim (N : Total) return String is
      Img : constant String := Total'Image (N);
   begin
      return Img (Img'First + 1 .. Img'Last);
   end Trim;

   function Hundredths (Value_Times_100 : Total) return String is
      Cents : constant Total := Value_Times_100 mod 100;
   begin
      return Trim (Value_Times_100 / 100) & "." & (if Cents < 10 then "0" else "") & Trim (Cents);
   end Hundredths;

   function To_Json (S : Summary) return String is
      Ended : constant Total := Total (S.Games - S.Endless);
      Games : constant Total := Total (S.Games);
      --  Rounded average in hundredths: (100 * sum + ended / 2) / ended.
      function Mean (Sum : Total) return String is
        (if Ended = 0 then "0.00" else Hundredths ((100 * Sum + Ended / 2) / Ended));
      function Share (Part : Natural) return String is
        (Hundredths ((10_000 * Total (Part) + Games / 2) / Games));
      function Trim (N : Natural) return String is (Trim (Total (N)));
      LF : constant Character := Character'Val (10);
   begin
      return "{" & LF
        & "  ""games"": " & Trim (S.Games) & "," & LF
        & "  ""first_wins"": " & Trim (S.First_Wins) & "," & LF
        & "  ""second_wins"": " & Trim (S.Second_Wins) & "," & LF
        & "  ""draws"": " & Trim (S.Draws) & "," & LF
        & "  ""endless"": " & Trim (S.Endless) & "," & LF
        & "  ""first_wins_percent"": " & Share (S.First_Wins) & "," & LF
        & "  ""endless_percent"": " & Share (S.Endless) & "," & LF
        & "  ""plies_mean"": " & Mean (S.Plies_Total) & "," & LF
        & "  ""plies_shortest"": " & Trim (if Ended = 0 then 0 else S.Shortest) & "," & LF
        & "  ""plies_longest"": " & Trim (S.Plies_Max) & "," & LF
        & "  ""longest_seed"": " & Trim (S.Longest_Seed) & "," & LF
        & "  ""wars_mean"": " & Mean (S.Wars_Total) & LF
        & "}";
   end To_Json;

end Stats;
