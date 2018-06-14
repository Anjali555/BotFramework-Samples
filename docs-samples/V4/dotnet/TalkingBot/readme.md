# TalkingBot hosted in ASP.NET Core
This sample shows how to use Xunit, the TestAdapter, and a TestFlow to test a simple bot.

1. Build the project.
1. Run the tests (there's just one), and it's supposed to fail.

> **Note**: There are two checks in the test to demonstrate how test flow objects work. They are effectively immutable, so the first check fails to fail, and the second fails appropriately.
