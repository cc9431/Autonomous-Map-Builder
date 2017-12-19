using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

//Script used primarily to sort Nodes for A* path-planning
class MinHeap<T> where T : IComparable<T> {
    private List<T> array = new List<T>();      // List for holding generic item

    //"Bubble up" style of adding elements to min heap 
    public void Add(T element){
        array.Add(element);
        int c = array.Count - 1;
        //Items are added, then their correct place is found in log(n) time
        while (c > 0 && array[c].CompareTo(array[c / 2]) == -1){
            T tmp = array[c];
            array[c] = array[c / 2];
            array[c / 2] = tmp;
            c = c / 2;
        }
    }

    //Grab the item from top of the heap and return it
    //Then resort the heap
    public T RemoveMin(){
        //Swap min and max
        //Remove min
        T ret = array[0];
        array[0] = array[array.Count - 1];
        array.RemoveAt(array.Count - 1);

        //Allow max to trickle down to rightful place, sorting along the way 
        int c = 0;
        while (c < array.Count){
            int min = c;
            if (2 * c + 1 < array.Count && array[2 * c + 1].CompareTo(array[min]) == -1)
                min = 2 * c + 1;
            if (2 * c + 2 < array.Count && array[2 * c + 2].CompareTo(array[min]) == -1)
                min = 2 * c + 2;

            if (min == c)
                break;
            else{
                T tmp = array[c];
                array[c] = array[min];
                array[min] = tmp;
                c = min;
            }
        }

        return ret;
    }

    //Return the min without removing it
    public T Peek(){
        return array[0];
    }

    //Return the size of the heap
    public int Count{
        get{
            return array.Count;
        }
    }
}
