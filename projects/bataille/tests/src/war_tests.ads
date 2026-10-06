--  Unit tests of the cards, the piles and the game of War.
with AUnit;
with AUnit.Test_Cases;

package War_Tests is

   type Test is new AUnit.Test_Cases.Test_Case with null record;

   overriding procedure Register_Tests (T : in out Test);
   overriding function Name (T : Test) return AUnit.Message_String;

end War_Tests;
