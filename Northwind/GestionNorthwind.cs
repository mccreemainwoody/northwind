using Microsoft.EntityFrameworkCore;
using Northwind.Models;

namespace Northwind;

public class GestionNorthwind
{
    private northwindContext _context = new northwindContext();

    #region Partie 2 - CRUD - Produits
    public List<Product> GetProduits() => _context.Products.ToList();
    public Product? GetProduit(int id) => _context.Products.Find(id);
    public Product? GetProduit(string nom) => _context.Products.FirstOrDefault(p => p.ProductName == nom);

    public Product? AjouterProduit(Product produit)
    {
        _context.Products.Add(produit ?? throw new ArgumentNullException(nameof(produit)));
        return _context.SaveChanges() > 0 ? produit : null;
    }
    public bool AjouterProduits(List<Product> produits)
    {
        //Potentiellement moins sécurisé, mais plus rapide qu'appeler AjouterProduit plusieurs fois
        _context.Products.AddRange(produits ?? throw new ArgumentNullException(nameof(produits)));
        return _context.SaveChanges() > 0;
    }

    public Product? ModifierProduit(Product produit, bool save = true)
    {
        if (produit == null) throw new ArgumentNullException(nameof(produit));
        _context.Entry(produit).State = EntityState.Modified;
        return !save ? null : _context.SaveChanges() > 0 ? produit : null;
        /*
         * On utilise ce paramètre save pour éviter de lancer SaveChanges() plusieurs fois dans certaines situations,
         * comme par exemple dans la fonction AjouterOrder lorsqu'elle appelle la fonction ModifierProduitStock
         * pour affilier chaque produit à la commande.
         */
    }
    private void ModifierProduitStock(Product produit, int quantite)
    {
        produit.UnitsInStock -= quantite;
        produit.UnitsOnOrder += quantite;
        ModifierProduit(produit, false);
    }

    public bool SupprimerProduit(int id) => SupprimerProduit(GetProduit(id));
    public bool SupprimerProduit(Product produit)
    {
        if (produit == null) throw new ArgumentNullException(nameof(produit));
        _context.Products.Remove(produit);
        return _context.SaveChanges() > 0;
    }
    public bool SupprimerProduits(List<Product> produits)
    {
        if (produits == null) throw new ArgumentNullException(nameof(produits));
        _context.Products.RemoveRange(produits);
        return _context.SaveChanges() > 0;
    }
    #endregion

    #region Partie 3 - AjouterOrder
    private Order? AjouterJusteOrder(Order order)
    {
        if (order == null) throw new ArgumentNullException(nameof(order));
        _context.Orders.Add(order);
        return _context.SaveChanges() > 0 ? order : null;
    }

    public void AjouterOrder(List<Product> produits)
    {
        if (produits == null || produits.Count == 0) throw new ArgumentException("Vous devez préciser au moins un produit pour créer une commande !");
        int commandeId = AjouterJusteOrder(new Order { OrderDate = DateTime.Now })?.OrderId ?? throw new Exception("Une erreur est survenue lors de la création de la commande.");
        Console.WriteLine("Création de la commande n°" + commandeId);
        
        foreach (var produit in
                 from p in produits
                 group p by p.ProductId into g
                 select g)
        {
            _context.Orderdetails.Add(new Orderdetail
            {
                OrderId = commandeId,
                ProductId = produit.Key,
                Quantity = produit.Count(),
                UnitPrice = produit.First().UnitPrice.GetValueOrDefault(),
                Discount = 0
            });
            ModifierProduitStock(produit.First(), produit.Count());
        }

        _context.SaveChanges();
    }
    #endregion


    #region Partie 1 - Exercices de requêtes

    #region Définitions de fonctions privées
    /**
     * <summary>
     *      Récupère tous les éléments d'un type d'entités de la base de données, en appliquant les conditions et liaisons
     *      d'ensembles d'entités précisées en paramètres.
     * </summary>
     * <example>
     *    On souhaite récupérer la commande de 200€ ou plus, à partir de la table orderdetails :
     *    <code>
     *         Select&lsaquo;Orderdetails&rsaquo;(od => od.OrderIdNavigation.UnitPrice >= 200, "OrderIdNavigation");
     *    </code>
     * </example>
     * <typeparam name="TInput">Le type d'entité interrogée. Sa déclaration est obligatoire.</typeparam>
     * <param name="condition">La condition à appliquer.</param>
     * <param name="join">Les ensembles à associer à l'ensembles d'entités interrogés.
     * Les noms doivent se terminer par IdNavigation et sont séparés d'une virgule si on doit lier plusieurs ensembles.</param>
     * <returns>La liste de résultat de la requête. Elle est déjà traitée pour ne contenir que des éléments distincts.</returns>
     * <seealso cref="Select{TInput, TOutput}(x,x,x)"/>
     * <seealso cref="SelectGroupBy{TInput, TKey}(x,x)"/>
     */
    private List<TInput> Select<TInput>(Func<TInput, bool>? condition = null, string? join = null) where TInput : class
    /*
     * On réalise l'ici l'enchaînement de tous les éléments nécessaires en une fois pour éviter d'occuper de
     * l'espace mémoire inutilement (séparer les étapes demanderait 3 (re)définitions de variables locales).
     * L'enchaînement, réalisé dans l'ordre, est le suivant :
     * 
     *      - On rassemble tous les ensembles à lier dans un tableau afin de pouvoir utiliser le pouvoir de
     *        la fonction Aggregate() (aussi appelable depuis une liste vide, ce qui permet de traiter la
     *        situation où il n'y a pas de liaison à faire).
     * 
     *      - On appelle l'ensemble d'entités principal (TInput), et on le convertit en IQueryable<TInput> 
     *        afin de pouvoir utiliser la fonction Include() (Include() retourne un objet IQueryable. Sans la
     *        conversion, il est incapable d'affecter sa valeur au résultat current, alors de type DbSet).
     * 
     *      - Une fois tous les préparatifs, on utilise la fonction Aggregate pour exécuter tous les
     *        appels de Include() nécessaires, liant ainsi les ensembles d'entités demandés.
     * 
     *      - On tourne le type IQueryable de l'objet en IEnumerable pour faciliter la suite de son traitement,
     *        puis on applique respectivement la condition souhaitée, le filtre d'éléments distincts, et la
     *        conversion en List.
     *        Remarque : Si aucune condition n'est donnée, on retourne toutes les entités de l'ensemble
     *
     *      - On retourne le résultat final.
     */
        => (join?.Split(',') ?? Array.Empty<string>())
            .Aggregate(_context.Set<TInput>() as IQueryable<TInput>, (current, table) => current.Include(table))
            .AsEnumerable().Where(condition ?? (_ => true)).Distinct().ToList();

    /**
     * <summary>
     *      Récupère tous les éléments d'un type d'entités de la base de données, en appliquant les conditions et liaisons
     *      de table précisés en paramètres. Est conçu pour les requêtes qui demandent à retourner qu'une partie des éléments
     *      de l'entité (requête SELECT non * en SQL).
     * </summary>
     * <example>
     *    On souhaite récupérer la commande n°1, depuis l'ensemble orderdetails
     *    <code>
     *         Select&lsaquo;Orderdetails, Order&rsaquo;(od => od.OrderId == 1, od => od.OrderIdNavigation, "OrderIdNavigation");
     *    </code>
     * </example>
     * <typeparam name="TInput">Le type d'entité interrogée. Sa déclaration est obligatoire.</typeparam>
     * <typeparam name="TOutput">Le type d'entité de sortie.</typeparam>
     * <param name="condition">La condition à appliquer.</param>
     * <param name="select">L'élément ou les éléments de l'Entité à extracter. Le paramètre s'exprime de la même manière
     * que celui du paramètre selector de la fonction LINQ Select : objet => objet.attribut</param>
     * <param name="join">Les ensembles à associer à l'ensembles d'entités interrogés.
     * Les noms doivent se terminer par IdNavigation et sont séparés d'une virgule si on doit lier plusieurs ensembles.</param>
     * <returns>La liste de résultat de la requête. Elle est déjà traitée pour ne contenir que des éléments distincts.</returns>
     * <exception cref="ArgumentNullException">Si l'appel de cette fonction a été forcée sans préciser de paramètres.</exception>
     * <seealso cref="Select{TInput}(Func{TInput, bool}, string)"/>
     * <seealso cref="SelectGroupBy{TInput, TKey}(x,x)"/>
     */
    private List<TOutput> Select<TInput, TOutput>(Func<TInput, TOutput> select, Func<TInput, bool>? condition, string? join = null) where TInput : class
        => select != null
            ? Select(condition, join).Select(select).Distinct().ToList()
            : throw new ArgumentNullException(nameof(select));

    /**
     * <summary>
     *      Une fonction générique Select, mais adaptée pour les requêtes de type GroupBy.
     * </summary>
     * <example>
     *    On souhaite rassembler tous les Ordersdetails et leurs produits.
     *    <code>
     *         SelectGroupBy(od => od.OrderId);
     *    </code>
     * </example>
     * <typeparam name="TInput">Le type d'entité interrogée. Sa déclaration est obligatoire.</typeparam>
     * <typeparam name="TKey">Le type de l'attribut à l'origine du GroupBy.</typeparam>
     * <param name="groupby">L'attribut centralisant les entités concernés.</param>
     * <param name="condition">La condition à appliquer.</param>
     * <param name="join">Les ensembles à associer à l'ensembles d'entités interrogés.
     * Les noms doivent se terminer par IdNavigation et sont séparés d'une virgule si on doit lier plusieurs ensembles.</param>
     * <returns>Un ensemble d'éléments IGrouping regroupant tous les résultats du GroupBy.</returns>
     * <seealso cref="Select{TInput}(Func{TInput, bool}, string)"/>
     * <seealso cref="Select{TInput, TOutput}(x,x,x)"/>
     */
    private List<IGrouping<TKey, TInput>> SelectGroupBy<TInput, TKey>(Func<TInput, TKey> groupby,
        Func<IGrouping<TKey, TInput>, bool>? condition = null, string? join = null) where TInput : class
        => (join?.Split(',') ?? Array.Empty<string>())
            .Aggregate(_context.Set<TInput>() as IQueryable<TInput>, (current, table) => current.Include(table))
            .AsEnumerable().GroupBy(groupby).Where(condition ?? (_ => true)).ToList();
    
    /**
     * <summary>
     *      Retourne un élément spécifique d'une liste retourné par SelectGroupBy.
     * </summary>
     * <example>
     *    On souhaite rassembler tous les Ordersdetails et leurs produits.
     *    <code>
     *         SelectGroupBy(od => od.OrderId);
     *    </code>
     * </example>
     * <typeparam name="TInput">Le type d'entité interrogée. Sa déclaration est obligatoire.</typeparam>
     * <typeparam name="TKey">Le type de l'attribut à l'origine du GroupBy.</typeparam>
     * <typeparam name="TOutput">Le type d'entité de sortie.</typeparam>
     * <param name="groupby">L'attribut centralisant les entités concernés.</param>
     * <param name="select">L'élément ou les éléments de l'Entité à extracter. Le paramètre s'exprime de la même manière
     * que celui du paramètre selector de la fonction LINQ Select : objet => objet.attribut</param>
     * <param name="condition">La condition à appliquer.</param>
     * <param name="join">Les ensembles à associer à l'ensembles d'entités interrogés.
     * Les noms doivent se terminer par IdNavigation et sont séparés d'une virgule si on doit lier plusieurs ensembles.</param>
     * <returns>Un ensemble d'éléments IGrouping regroupant tous les résultats du GroupBy.</returns>
     * <seealso cref="Select{TInput}(Func{TInput, bool}, string)"/>
     * <seealso cref="Select{TInput, TOutput}(x,x,x)"/>
     */
    private List<TOutput> SelectGroupBy<TInput, TKey, TOutput>(Func<TInput, TKey> groupby, Func<IGrouping<TKey, TInput>, TOutput> select, 
        Func<IGrouping<TKey, TInput>, bool>? condition = null, string? join = null) where TInput : class
        => SelectGroupBy(groupby, condition, join).Select(select ?? throw new ArgumentNullException(nameof(select)))
            .Distinct().ToList();
    
    /**
     * <summary>
     *      Raccourci pour l'appel de la fonction SelectGroupBy(od, od => od.OrderId), permettant de rassembler tous les
     *      produits (symbolisés par ProductID) des commandes (OrderID).
     * </summary>
     * <param name="condition">La condition à appliquer.</param>
     * <param name="join">Les ensembles à associer à l'ensembles d'entités interrogés.
     * Les noms doivent se terminer par IdNavigation et sont séparés d'une virgule si on doit lier plusieurs ensembles.</param>
     * <seealso cref="SelectGroupBy{TInput, TKey}(x,x)"/>
     * <seealso cref="TotalCommandes{TOutput}(Func{IGrouping{int, Orderdetail}, TOutput}, Func{IGrouping{int, Orderdetail}, bool}, string)"/>
     * <seealso cref="TotalCommandesTotal(Func{IGrouping{int, Orderdetail}, bool}?, string?)"/>
     */
    private List<IGrouping<int, Orderdetail>> TotalCommandes(Func<IGrouping<int, Orderdetail>, bool>? condition = null, string? join = null)
        => SelectGroupBy(od => od.OrderId, condition, join);
    
    /**
     * <summary>
     *      Raccourci pour l'appel de la fonction SelectGroupBy(od, od => od.OrderId), permettant de rassembler tous les
     *      produits (symbolisés par ProductID) des commandes (OrderID).
     *      Retourne à la place l'élément précisé en paramètre.
     * </summary>
     * <typeparam name="TOutput">Le type d'entité de sortie.</typeparam>
     * <param name="select">L'élément ou les éléments de l'Entité à extracter. Le paramètre s'exprime de la même manière
     * que celui du paramètre selector de la fonction LINQ Select : objet => objet.attribut</param>
     * <param name="condition">La condition à appliquer.</param>
     * <param name="join">Les ensembles à associer à l'ensembles d'entités interrogés.
     * Les noms doivent se terminer par IdNavigation et sont séparés d'une virgule si on doit lier plusieurs ensembles.</param>
     * <seealso cref="SelectGroupBy{TInput, TKey}(x,x)"/>
     * <seealso cref="TotalCommandes(Func{IGrouping{int, Orderdetail}, bool}, string)"/>
     * <seealso cref="TotalCommandesTotal(Func{IGrouping{int, Orderdetail}, bool}?, string?)"/>
     */
    private List<TOutput> TotalCommandes<TOutput>(Func<IGrouping<int, Orderdetail>, TOutput> select,
        Func<IGrouping<int, Orderdetail>, bool>? condition = null, string? join = null)
        => SelectGroupBy(od => od.OrderId, select, condition, join);

    /**
     * <summary>
     *      Raccourci pour l'appel de la fonction SelectGroupBy(od, od => od.OrderId), permettant de rassembler tous les
     *      produits (symbolisés par ProductID) des commandes (OrderID).
     *      Retourne à la place une liste de tuples avec en valeurs l'id de la commande, l'objet de la commande et la
     *      somme monétaire totaire des produits de cette commande.
     * </summary>
     * <param name="condition">La condition à appliquer.</param>
     * <param name="join">Les ensembles à associer à l'ensembles d'entités interrogés.
     * Les noms doivent se terminer par IdNavigation et sont séparés d'une virgule si on doit lier plusieurs ensembles.</param>
     * <seealso cref="SelectGroupBy{TInput, TKey}(x,x)"/>
     * <seealso cref="TotalCommandes(Func{IGrouping{int, Orderdetail}, bool}?, string)"/>
     * <seealso cref="TotalCommandes{TOutput}(Func{IGrouping{int, Orderdetail}, TOutput}, Func{IGrouping{int, Orderdetail}, bool}, string)"/>
     */
    private List<(int, Orderdetail, decimal)> TotalCommandesTotal(Func<IGrouping<int, Orderdetail>, bool>? condition = null, string? join = null)
        => TotalCommandes(od => (od.Key, od.First(), od.Sum(details => details.UnitPrice * details.Quantity)), 
            condition, join);
    #endregion
    
    #region Fonctions des requêtes demandées
    //Requête 1
    public List<Employee> RechercherEmployeesAnneeLieu(DateTime annee, string lieu)
        => RechercherEmployeesAnneeLieu(annee.Year, lieu);

    public List<Employee> RechercherEmployeesAnneeLieu(int annee, string lieu)
        => Select<Employee>(em => em.City! == lieu && em.HireDate!.Value.Year == annee);
    
    //Requête 2
    public List<Employee> RechercherEmployeesTitre(string titre)
        => Select<Employee>(em => em.Title!.Contains(titre));
    
    //Requête 3
    public decimal MoyennePrixProduitsCategorie(string nomCategorie)
        => Math.Round(Select<Product, decimal?>(p => p.UnitPrice!, 
            p => p.CategoryIdNavigation!.CategoryName == nomCategorie, "CategoryIdNavigation")
            .Average().GetValueOrDefault(), 2);

    //Requête 4
    public List<Order> RechercherOrdersDate(DateTime date)
        => Select<Order>(c => c.OrderDate!.Value > date);

    //Requête 5
    public List<Order> RechercherOrdersPrixProduitMin(decimal prixMin)
        => TotalCommandes(cd => cd.First().OrderIdNavigation!,
                couple => couple.Any(details => details.UnitPrice > prixMin), 
                "OrderIdNavigation");

    //Requête 6
    public List<Product> RechercherProduitsNonCommandes()
        => Select<Product>(c => !new northwindContext().Orderdetails.Any(cd => cd.ProductId == c.ProductId));
    // Si on ne fait pas de new northwindContext(), on obtient une erreur indiquant que la connexion est déjà ouverte ?

    //Requête 7
    public List<Orderdetail> RechercherOrderdetailsEncadrementMax(double reductMin, double reductMax, decimal prixMax, int quantite = 1)
        => TotalCommandes(od => od.First(),
                od => od.All(details => details.UnitPrice < prixMax)
                      && od.Sum(details => details.Discount) >= reductMin
                      && od.Sum(details => details.Discount) <= reductMax
                      && od.All(details => details.Quantity > quantite));

    //Requête 8
    public List<Employee> RechercherEmployeesCommandeQuantiteProduit(int quantite)
        => TotalCommandes(od => od.First().OrderIdNavigation!.EmployeeIdNavigation!,
                couple => couple.Sum(od => od.Quantity) > quantite, 
                "OrderIdNavigation,OrderIdNavigation.EmployeeIdNavigation");
    #endregion

    #endregion
}