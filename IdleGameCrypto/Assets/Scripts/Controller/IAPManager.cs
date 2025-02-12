﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

[System.Serializable]
public struct ProductPurchase
{
    public string productNameApple;
    public string productNameGooglePlay;
}

// Deriving the Purchaser class from IStoreListener enables it to receive messages from Unity Purchasing.
public class IAPManager : MonoBehaviour, IStoreListener
{
    private static IStoreController m_StoreController;
    // The Unity Purchasing system.
    private static IExtensionProvider m_StoreExtensionProvider;
    // The store-specific Purchasing subsystems.

    public List<ProductPurchase> ProductListConsume;

    public List<ProductPurchase> ProductListNonConsume;

    private List<string> ProductName;

    private List<string> ProductNonName;

    public static IAPManager instance;

    private void Awake()
    {
        instance = this;
    }


    void Start()
    {
        // If we haven't set up the Unity Purchasing reference
        if (m_StoreController == null)
        {
            // Begin to configure our connection to Purchasing
            InitializePurchasing();
        }
    }

    public void InitializePurchasing()
    {
        // If we have already connected to Purchasing ...
        if (IsInitialized())
        {
            // ... we are done here.
            return;
        }


        // Create a builder, first passing in a suite of Unity provided stores.
        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());


        ProductName = new List<string>();
        for (int i = 0; i < ProductListConsume.Count; i++)
        {
            ProductName.Add("consume " + i.ToString());
            builder.AddProduct(ProductName[i], ProductType.Consumable, new IDs() { {
                    ProductListConsume [i].productNameApple,
                    AppleAppStore.Name
                },
                 {
                    ProductListConsume [i].productNameGooglePlay,
                    GooglePlay.Name
                },
            });
        }


        ProductNonName = new List<string>();
        for (int i = 0; i < ProductListNonConsume.Count; i++)
        {
            ProductNonName.Add("Nonconsume " + i.ToString());
            builder.AddProduct(ProductNonName[i], ProductType.NonConsumable, new IDs() { {
                    ProductListNonConsume [i].productNameApple,
                    AppleAppStore.Name
                },
                 {
                    ProductListNonConsume [i].productNameGooglePlay,
                    GooglePlay.Name
                },
            });
        }
        // Kick off the remainder of the set-up with an asynchrounous call, passing the configuration 
        // and this class' instance. Expect a response either in OnInitialized or OnInitializeFailed.
        UnityPurchasing.Initialize(this, builder);


    }


    private bool IsInitialized()
    {
        // Only say we are initialized if both the Purchasing references are set.
        return m_StoreController != null && m_StoreExtensionProvider != null;
    }


    public void BuyProductConsume(int indexProduct)
    {
        BuyProductID(ProductName[indexProduct]);

    }

	public void BuyProductNonConsume(int indexProduct)
	{
		BuyProductID(ProductNonName[indexProduct]);

	}

    void BuyProductID(string productId)
    {
        // If Purchasing has been initialized ...
        if (IsInitialized())
        {
            // ... look up the Product reference with the general product identifier and the Purchasing 
            // system's products collection.
            Product product = m_StoreController.products.WithID(productId);

            // If the look up found a product for this device's store and that product is ready to be sold ... 
            if (product != null && product.availableToPurchase)
            {
                Debug.Log(string.Format("Purchasing product asychronously: '{0}'", product.definition.id));
                // ... buy the product. Expect a response either through ProcessPurchase or OnPurchaseFailed 
                // asynchronously.
                m_StoreController.InitiatePurchase(product);
            }
            // Otherwise ...
            else
            {
                // ... report the product look-up failure situation  
                Debug.Log("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
            }
        }
        // Otherwise ...
        else
        {
            // ... report the fact Purchasing has not succeeded initializing yet. Consider waiting longer or 
            // retrying initiailization.
            Debug.Log("BuyProductID FAIL. Not initialized.");
        }
    }


    // Restore purchases previously made by this customer. Some platforms automatically restore purchases, like Google.
    // Apple currently requires explicit purchase restoration for IAP, conditionally displaying a password prompt.
    public void RestorePurchases()
    {


        // If Purchasing has not yet been set up ...
        if (!IsInitialized())
        {
            // ... report the situation and stop restoring. Consider either waiting longer, or retrying initialization.
            Debug.Log("RestorePurchases FAIL. Not initialized.");
            return;
        }

        // If we are running on an Apple device ... 
        if (Application.platform == RuntimePlatform.IPhonePlayer ||
             Application.platform == RuntimePlatform.OSXPlayer)
        {
            // ... begin restoring purchases
            Debug.Log("RestorePurchases started ...");

            // Fetch the Apple store-specific subsystem.
            var apple = m_StoreExtensionProvider.GetExtension<IAppleExtensions>();
            // Begin the asynchronous process of restoring purchases. Expect a confirmation response in 
            // the Action<bool> below, and ProcessPurchase if there are previously purchased products to restore.
            apple.RestoreTransactions((result) =>
            {
                // The first phase of restoration. If no more responses are received on ProcessPurchase then 
                // no purchases are available to be restored.
                Debug.Log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
            });
        }
        // Otherwise ...
        else
        {
            // We are not running on an Apple device. No work is necessary to restore purchases.
            Debug.Log("RestorePurchases FAIL. Not supported on this platform. Current = " + Application.platform);
        }

    }


    //
    // --- IStoreListener
    //

    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        // Purchasing has succeeded initializing. Collect our Purchasing references.
        Debug.Log("OnInitialized: PASS");

        // Overall Purchasing system, configured with products for this application.
        m_StoreController = controller;
        // Store specific subsystem, for accessing device-specific store features.
        m_StoreExtensionProvider = extensions;
    }


    public void OnInitializeFailed(InitializationFailureReason error)
    {
        // Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
        Debug.Log("OnInitializeFailed InitializationFailureReason:" + error);
    }


    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        for (int i = 0; i < ProductName.Count; i++)
        {
            if (String.Equals(args.purchasedProduct.definition.id, ProductName[i], StringComparison.Ordinal))
            {
                //Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));//If the consumable item has been successfully purchased, add 100 coins to the player's in-game score.
                //ProductList[i].eventPurchaseComplete.Invoke ();
                AddMoney(i);
            }
            else
            {
                Debug.Log(string.Format("ProcessPurchase: FAIL. Unrecognized product: '{0}'", args.purchasedProduct.definition.id));
            }// Return a flag indicating wither this product has completely been received, or if the application needs to be reminded of this purchase at next app launch. Is useful when saving purchased products to the cloud, and when that save is delayed.
        }

        for (int i = 0; i < ProductNonName.Count; i++)
        {
            if (String.Equals(args.purchasedProduct.definition.id, ProductNonName[i], StringComparison.Ordinal))
            {
                //Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));//If the consumable item has been successfully purchased, add 100 coins to the player's in-game score.
                //ProductList[i].eventPurchaseComplete.Invoke ();
                AdsControl.Instance.HideBanner();
                PlayerPrefs.SetInt("RemoveAds", 1);
            }
            else
            {
                Debug.Log(string.Format("ProcessPurchase: FAIL. Unrecognized product: '{0}'", args.purchasedProduct.definition.id));
            }// Return a flag indicating wither this product has completely been received, or if the application needs to be reminded of this purchase at next app launch. Is useful when saving purchased products to the cloud, and when that save is delayed.
        }

        return PurchaseProcessingResult.Complete;
    }


    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        // A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing 
        // this reason with the user to guide their troubleshooting actions.
        Debug.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
    }

    void AddMoney(int _index)
    {
        switch (_index)
        {

            case 0:
                GamePlaySytem.instance.marketSystem.AddGems(80);
                break;
            case 1:
                GamePlaySytem.instance.marketSystem.AddGems(500);
                
                break;
            case 2:
                GamePlaySytem.instance.marketSystem.AddGems(1200);
               
                break;
            case 3:
                GamePlaySytem.instance.marketSystem.AddGems(2500);
               
                break;
            case 4:
                GamePlaySytem.instance.marketSystem.AddGems(6500);
                
                break;
            case 5:
                GamePlaySytem.instance.marketSystem.AddGems(1400);
                break;
        }
        GamePlaySytem.instance._saveSystem.Save();
    }


}
