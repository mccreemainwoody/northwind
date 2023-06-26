/*
using Microsoft.EntityFrameworkCore;
using Northwind.Models;

void Exercice(int n)
{
        Console.WriteLine(n);
}

northwindContext db = new northwindContext();

Exercice(1);
foreach (var em in 
         from em in db.Employees
         where em.City == "London" && em.HireDate.Value.Year == 1994
         select new List<string> { em.FirstName, em.LastName } 
         ) Console.WriteLine(em);

Exercice(2);
foreach(var em in db.Employees.Where(em => em.Title.Contains("Representative"))) Console.WriteLine(em.FirstName, em.LastName);

Exercice(3);
/*
 * Insérer la moyenne de la requête sans contrôle de sa valeur crée une erreur de type à la compilation,
 * car la classe Math "n'aime pas" l'idée que la requête peut retourner une valeur null (ce qui indiqué par
 * le type Nullable<T>).
 * Pour contourner ce problème,  nous utiliserons la méthode GetValueOrDefault() de la classe Nullable<T>,
 * créée spécialement pour ça et qui retourne la valeur de la propriété Average ou la valeur par défaut du
 * type de la propriété (ici 0).
 * Il existe néanmoins de nombreuses solutions différentes pour palier à ce problème, comme par exemple se
 * contenter de ne retourner que 0 si la requête ne retourne rien. ([query] ?? 0)
 *//*
Console.WriteLine(Math.Round(( 
    from p in db.Products.Include(c => c.CategoryIdNavigation)
    where p.CategoryIdNavigation.CategoryName == "Seafood"
    select p.UnitPrice
    ).Average().GetValueOrDefault(),2));

Exercice(4);
foreach(var c in
        from c in db.Orders
        where c.OrderDate.Value > new DateTime(1996,6,2)
        select new {c.OrderId, c.OrderDate}
        ) Console.WriteLine(c);

Exercice(5);
foreach(var c in
        from info in db.Orders
        join details in db.Orderdetails on info.OrderId equals details.OrderId
        where details.UnitPrice > 230
        select new {info.OrderId, info.OrderDate, details.UnitPrice}
       ) Console.WriteLine(c);

Exercice(6);
foreach(var c in
        from c in db.Orders
        where !db.Orderdetails.Any(d => d.OrderId == c.OrderId)
        select new {c.OrderId, c.OrderDate}
        ) Console.WriteLine(c);

Exercice(7);
foreach(var c in
        from c in db.Orderdetails
        where c.UnitPrice < 20 && c.Quantity > 40 && c.Discount >= 0.2 && c.Discount <= 0.3
        select new {c.OrderId, c.UnitPrice, c.Quantity, c.Discount}
        ) Console.WriteLine(c);

Exercice(8);
foreach(var em in
        from em in db.Employees
        where db.Orderdetails.Any(od => od.OrderIdNavigation.EmployeeId == em.EmployeeId && od.Quantity > 120)
        select em.FirstName + ' ' + em.LastName
        ) Console.WriteLine(em);
*/