--  Runs every test; the exit status is non-zero if one fails, so CI goes red.
with AUnit;             use type AUnit.Status;
with AUnit.Reporter.Text;
with AUnit.Run;
with AUnit.Test_Suites; use AUnit.Test_Suites;
with War_Tests;

procedure Test_Main is
   Tests : aliased War_Tests.Test;

   function Suite return Access_Test_Suite is
      Result : constant Access_Test_Suite := New_Suite;
   begin
      Result.Add_Test (Tests'Access);
      return Result;
   end Suite;

   function Run is new AUnit.Run.Test_Runner_With_Status (Suite);
   Reporter : AUnit.Reporter.Text.Text_Reporter;
begin
   if Run (Reporter) /= AUnit.Success then
      raise Program_Error with "tests failed";
   end if;
end Test_Main;
