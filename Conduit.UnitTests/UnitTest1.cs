// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using FluentAssertions;

namespace Genova.Conduit.UnitTests;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        "Required tests are running".Should().NotBeNull();
    }
}
