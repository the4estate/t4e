#if UNITY_INCLUDE_TESTS
using System;
using System.Linq;
using NUnit.Framework;

public class DomainReferencesTests
{
    [Test]
    public void Domain_Assembly_Has_No_UnityEngine_References()
    {
        var domainAsm = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a =>
                a.GetName().Name.EndsWith(".Domain", StringComparison.OrdinalIgnoreCase) ||
                a.GetName().Name.Equals("T4E.Domain", StringComparison.OrdinalIgnoreCase) ||
                a.GetName().Name.Contains("Domain", StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(domainAsm, "Could not find the Domain assembly. Check asmdef name (T4E.Domain?).");

        var refs = domainAsm.GetReferencedAssemblies().Select(n => n.Name).ToArray();
        Assert.False(refs.Any(n => n.StartsWith("UnityEngine", StringComparison.OrdinalIgnoreCase)),
            "Domain references UnityEngine.*  → fix asmdef dependencies.");
    }
}
#endif
