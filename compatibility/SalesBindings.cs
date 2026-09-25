using Mono.Cecil;
using Mono.Cecil.Cil;
namespace BD2Territory.Compatibility;
public static class SalesBindings
{
 public static Dictionary<string,MethodDefinition> Resolve(ResolvedBindings r)
 {
  var inventory=r.Types[r.Contract.Roles["Inventory"]];
  var tables=BindingResolver.Api(r,r.Contract.Apis.Single(a=>a.Role=="Tables.Cook")).DeclaringType;
  bool Parameters(MethodDefinition m,params string[] types)=>m.Parameters.Select(p=>p.ParameterType.FullName).SequenceEqual(types);
  var send=inventory.Methods.Single(m=>m.IsStatic&&m.ReturnType.FullName=="System.Void"&&m.Parameters.Count==2&&m.Parameters[0].ParameterType.FullName=="System.Collections.Generic.List`1<Proto.Net.SellItemInfo>"&&m.Parameters[1].ParameterType.FullName.StartsWith("System.Action`1<System.Collections.Generic.List`1<",StringComparison.Ordinal)&&m.HasBody&&m.Body.Instructions.Any(i=>i.OpCode.Code==Code.Newobj&&i.Operand is MethodReference mr&&mr.DeclaringType.FullName=="Proto.Net.LifeShopSellRequest"));
  var rows=tables.Methods.Single(m=>m.IsStatic&&m.ReturnType.FullName=="System.Collections.Generic.List`1<Proto.Design.common.LifeSellItemTable>"&&Parameters(m,"System.Int32"));
  // These are the native shop category and territory item category, not ordinary backpack items.
  var category=tables.NestedTypes.Single(t=>t.IsEnum&&new[]{"None","Material","Product","Cooking","Favorite"}.All(n=>t.Fields.Any(f=>f.Name==n)));
  var group=tables.NestedTypes.Single(t=>t.IsEnum&&new[]{"None","Consumable","Making"}.All(n=>t.Fields.Any(f=>f.Name==n)));
  foreach(var pair in new[]{("Material",1),("Product",2),("Cooking",3)})if(Convert.ToInt32(category.Fields.Single(f=>f.Name==pair.Item1).Constant)!=pair.Item2)throw new InvalidOperationException("Territory item categories have changed");
  if(Convert.ToInt32(group.Fields.Single(f=>f.Name=="Consumable").Constant)!=1)throw new InvalidOperationException("Territory shop categories have changed");
  return new(){["Sales.Send"]=send,["Sales.Rows"]=rows};
 }
}
