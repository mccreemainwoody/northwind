using System.Collections;
using Microsoft.EntityFrameworkCore;
using Northwind;
using Northwind.Models;

void Exo(ValueType n) => Console.WriteLine($"\nExercice {n}");

#region Partie 1 - Sans GestionNorthwind
northwindContext db = new northwindContext();
void ExerciceA<T>(int n, IEnumerable<List<T>> result) //IEnumerable est nécessaire pour utiliser Aggregate() dans la compilation
{
    Exo(n);
    foreach (var item in result) Console.WriteLine(item.Aggregate("", (resultat, element) => $"{resultat}{element} "));
}
void ExerciceAValeurUnique(int n, ValueType result)
{
    Exo(n);
    Console.WriteLine(result);
}

ExerciceA(1, from em in db.Employees
              where em.City == "London" && em.HireDate.Value.Year == 1994
              select northwindContext.Composition(em.FirstName, em.LastName));

ExerciceA(2, from em in db.Employees
              where em.Title.Contains("Representative")
              select northwindContext.Composition(em.FirstName, em.LastName));

/*
 * Insérer la moyenne de la requête sans contrôle de sa valeur crée une erreur de type à la compilation,
 * car la classe Math "n'aime pas" l'idée que la requête peut retourner une valeur null (ce qui indiqué par
 * le type Nullable<T>).
 * Pour contourner ce problème,  nous utilisons la méthode GetValueOrDefault() de la classe Nullable<T>,
 * créée spécialement pour ça et qui retourne la valeur de la propriété Average ou la valeur par défaut du
 * type de la propriété (ici 0).
 * Il existe néanmoins de nombreuses solutions différentes pour palier à ce problème, comme par exemple se
 * contenter de ne retourner que 0 si la requête ne retourne rien. ([query] ?? 0)
 */

ExerciceAValeurUnique(3, Math.Round(( 
    from p in db.Products.Include(c => c.CategoryIdNavigation)
    where p.CategoryIdNavigation.CategoryName == "Seafood"
    select p.UnitPrice
).Average().GetValueOrDefault(),2));

ExerciceA(4, from c in db.Orders
              where c.OrderDate.Value > new DateTime(1996,6,2)
              select northwindContext.Composition<ValueType>(c.OrderId, c.OrderDate));

ExerciceA(5, from info in db.Orders
              join details in db.Orderdetails on info.OrderId equals details.OrderId
              where details.UnitPrice > 230
              select northwindContext.Composition<ValueType>(info.OrderId, info.OrderDate, Math.Round(details.UnitPrice,2)));

ExerciceA(6, from c in db.Orders
              where !db.Orderdetails.Any(d => d.OrderId == c.OrderId)
              select northwindContext.Composition<ValueType>(c.OrderId, c.OrderDate));

ExerciceA(7, from c in db.Orderdetails
              where c.UnitPrice < 20 && c.Quantity > 40 && c.Discount >= 0.2 && c.Discount <= 0.3
              select northwindContext.Composition<ValueType>(c.OrderId, Math.Round(c.UnitPrice,2), c.Quantity, c.Discount));

ExerciceA(8, from em in db.Employees
              where db.Orderdetails.Any(od => od.OrderIdNavigation.EmployeeId == em.EmployeeId && od.Quantity > 120)
              select northwindContext.Composition(em.FirstName, em.LastName));
#endregion 

// ----------------------------

#region Partie 2 - Avec GestionNorthwind

GestionNorthwind gestionnaire = new GestionNorthwind();
void ExerciceB<T>(double n, List<T> result) where T : class
{
    Exo(n);
    foreach (var item in result) Console.WriteLine(item);
}

ExerciceB(9.1, gestionnaire.RechercherEmployeesAnneeLieu(new DateTime(1994,1,1), "London"));
ExerciceB(9.2, gestionnaire.RechercherEmployeesTitre("Representative"));
Exo(9.3); Console.WriteLine(gestionnaire.MoyennePrixProduitsCategorie("Seafood"));
ExerciceB(9.4, gestionnaire.RechercherOrdersDate(new DateTime(1996,6,2)));
ExerciceB(9.5, gestionnaire.RechercherOrdersPrixProduitMin(230));
ExerciceB(9.6, gestionnaire.RechercherProduitsNonCommandes());
ExerciceB(9.7, gestionnaire.RechercherOrderdetailsEncadrementMax(0.2, 0.3, 20, 40));
ExerciceB(9.8, gestionnaire.RechercherEmployeesCommandeQuantiteProduit(120));

gestionnaire.AjouterOrder(new List<Product> {
    gestionnaire.GetProduit("Aniseed Syrup")!, 
    gestionnaire.GetProduit("Queso Manchego La Pastora")!, 
    gestionnaire.GetProduit("Aniseed Syrup")!
});
#endregion