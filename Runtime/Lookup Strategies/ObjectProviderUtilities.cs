﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Pickle.ObjectProviders
{
    public static class ObjectProviderUtilities
    {
        public static IObjectProvider ResolveProviderTypeToProvider(this ObjectProviderType providerType, System.Type fieldType, UnityEngine.Object owner = null)
        {
            List<IObjectProvider> strategiesForUnion = new List<IObjectProvider>();

            // find the owner gameobject
            // if the owner is not a gameobject or a component, we later skip looking at children / scene objects
            GameObject ownerGameObject = null;
            if (owner)
            {
                if (owner is Component comp)
                    ownerGameObject = comp.gameObject;
                else if (owner is GameObject go)
                    ownerGameObject = go;
            }

            var isFieldTypeAComponent = typeof(Component).IsAssignableFrom(fieldType);
            var isFieldTypeASceneObject = typeof(GameObject) == fieldType || isFieldTypeAComponent;

            if ((providerType & ObjectProviderType.Assets) != 0)
            {
                if (isFieldTypeAComponent)
                {
                    strategiesForUnion.Add(new PrefabComponentObjectProvider(fieldType));
                }
                else
                {
                    strategiesForUnion.Add(new AssetObjectProvider(fieldType));
                }
            }

            if (ownerGameObject && isFieldTypeASceneObject)
            {
                if ((providerType & ObjectProviderType.Children) != 0)
                {
                    if (isFieldTypeAComponent)
                    {
                        strategiesForUnion.Add(new ChildComponentsProvider(ownerGameObject.transform, fieldType));
                    }
                    else
                    {
                        strategiesForUnion.Add(new ChildObjectsProvider(ownerGameObject.transform));
                    }
                }

                if ((providerType & ObjectProviderType.Scene) != 0)
                {
                    if (isFieldTypeAComponent)
                    {
                        strategiesForUnion.Add(new SceneComponentsProvider(ownerGameObject.scene, fieldType));
                    }
                    else
                    {
                        strategiesForUnion.Add(new SceneObjectsProvider(ownerGameObject.scene));
                    }
                }
            }

            return new ObjectProviderUnion(strategiesForUnion.ToArray());
        }

        public static IObjectProvider GetDefaultObjectProviderForType(System.Type fieldType, UnityEngine.Object owner = null)
        {
            return (ObjectProviderType.Assets | ObjectProviderType.Scene).ResolveProviderTypeToProvider(fieldType, owner);
        }
    }
}