using System;
using System.Collections.Generic;
using System.Text;

namespace CaptchaOCR.AppEntry.Engine.IPCore
{
    using IntCollection = List<int>;
    using System.Collections;

    internal class ConnectedComponentProcessor
    {
        #region Connected Component Structures
        private class RunItem
        {
            public int Row;
            public int StartCol;
            public int EndCol;
            public RunItem(int _row, int _start_col, int _end_col)
            {
                Row = _row;
                StartCol = _start_col;
                EndCol = _end_col;
            }

            public override string ToString()
            {
                return string.Format("{0},{1},{2}", Row, StartCol, EndCol);
            }
        }
        private class EquivItem
        {
            public int K;
            public int P;
            public EquivItem(int _k, int _p)
            {
                K = _k;
                P = _p;
            }
            public override bool Equals(object obj)
            {
                if (((obj as EquivItem).K == K && (obj as EquivItem).P == P) ||
                    ((obj as EquivItem).P == K && (obj as EquivItem).K == P))
                    return true;
                else
                    return false;
            }
            public EquivItem Clone()
            {
                return new EquivItem(K, P);
            }
            public override int GetHashCode()
            {
                return base.GetHashCode();
            }

            public override string ToString()
            {
                return string.Format("{0},{1}", K, P);
            }

        }
        private class RunQueueItem
        {
            public int FirstRun;
            public int LastRun;
            public RunQueueItem(int _first, int _last)
            {
                FirstRun = _first;
                LastRun = _last;
            }
            public RunQueueItem()
            {
                Reset();
            }
            public void Reset()
            {
                FirstRun = -1;
                LastRun = -1;
            }
        }
        private class RunQueue : IDisposable
        {
            private int _nElement;
            private int _current_idx;

            private RunQueueItem[] _storage;
            public RunQueue(int _nEl)
            {
                _nElement = _nEl;
                init();
            }
            protected void init()
            {
                _current_idx = 0;
                if (_storage == null)
                {
                    _storage = new RunQueueItem[_nElement];
                }
                for (int i = 0; i < _nElement; i++)
                    _storage[i] = new RunQueueItem();
            }
            public void Reset()
            {
                _current_idx = 0;
                for (int iqueue = 0; iqueue < _nElement; iqueue++)
                {
                    _storage[iqueue].FirstRun = _storage[iqueue].LastRun = -1;
                }
            }
            public RunQueueItem this[int i]
            {
                get
                {
                    if (i < 0 || i >= _nElement)
                        throw new System.Exception("Index was out of boudaries");
                    return _storage[(_current_idx + i) % _nElement];
                }
            }
            public void MoveIndexForwardAndReset(int _idelta)
            {
                for (int i = 0; i < _idelta; i++)
                    MoveIndexNextAndReset();
            }
            public void MoveIndexNextAndReset()
            {
                _storage[_current_idx].FirstRun = -1;
                _storage[_current_idx].LastRun = -1;
                _current_idx = (_current_idx + 1) % _nElement;
            }
            #region IDisposable Members

            public void Dispose()
            {
                if (_storage != null)
                {
                    Array.Clear(_storage, 0, _storage.Length);
                    _storage = null;
                }
            }

            #endregion
        }
        #endregion Connected Component Structures

        #region int GetConnectedComponent(bool[] data, int dataStride, float nDistance, ref int[] labelMatrix, ref int nClusters)
        /// <summary>
        /// Get connected component from a logical matrix
        /// </summary>
        /// <param name="data">Linearized data of an 2D matrix</param>
        /// <param name="dataStride">Data stride which is the width of the 2D matrix</param>
        /// <param name="fDistance">Maximum distance between two connected points</param>
        /// <param name="nClusters"><paramref name=""/> [out] Found cluster count</param>
        /// <param name="pointX">[out] X coordinates of all point of the input data that have true value</param>
        /// <param name="pointY">[out] Y coordinates of all point of the input data that have true value</param>
        /// <param name="labelMatrix">[out] An 1D matrix has the length of true point count.
        /// The value of each element indicates the 1-based index of the cluster which the element belongs to. 
        /// Zero value indicates that the element belongs to no cluster</param>
        /// <returns>0 - succeeded. Non-zero value, otherwise</returns>
        public static unsafe int GetConnectedComponent(bool[] data, int dataStride, float fDistance, ref int nClusters,
            ref int[] pointX, ref int[] pointY, ref int[] labelMatrix)
        {
            labelMatrix = null;
            nClusters = 0;
            if (data == null)
                return 0;
            if (fDistance < 1)
                return 1;
            int dataLength = data.Length;
            if (dataLength % dataStride != 0)
                throw new System.Exception(
                    string.Format("Data length {0} and stride {1} are incompatible", 
                    dataLength, dataStride));

            int nDistance = (int)Math.Floor(fDistance);

            int rowCount = dataLength / dataStride;
            int colCount = dataStride;

            if ((rowCount == 0) || (colCount == 0))
                return 2;
            int nPoints = 0;

            IntCollection xs = new IntCollection();
            IntCollection ys = new IntCollection();
            fixed (bool* pData = data)
            {
                bool* pwData = pData;
                for (int i = 0; i < rowCount; i++)
                    for (int j = 0; j < colCount; j++, pwData++)
                        if (*pwData)
                        {
                            nPoints++;
                            xs.Add(j);
                            ys.Add(i);
                        }
            }
            if (nPoints == 0)
                return 0;
            labelMatrix = new int[nPoints];
            pointX = xs.ToArray();
            pointY = ys.ToArray();

            int nRuns;
            ArrayList runs = new ArrayList();
            ArrayList countDefRun = new ArrayList();
            ArrayList equivList = new ArrayList();
            RunQueue runQueue = null;
            try
            {
                #region Fill runs
                int row = 0, col = 0, lastCol = 0, iPoint = 0;
                bool empty = false;

                fixed (bool* pData = data)
                {
                    bool* pwData = pData;
                    for (row = 0; row < rowCount; row++)
                    {
                        col = 0;
                        empty = true;
                        while (col < colCount)
                        {
                            if (*pwData)
                            {
                                if (empty)
                                {
                                    empty = false;
                                    runs.Add(new RunItem(row, col, col));
                                }
                                else
                                {
                                    if ((col - lastCol) > fDistance)// this point starts a new run
                                    {
                                        empty = false;
                                        (runs[runs.Count - 1] as RunItem).EndCol = lastCol;
                                        runs.Add(new RunItem(row, col, col));
                                    }
                                }
                                labelMatrix[iPoint++] = runs.Count;
                                lastCol = col;
                            }
                            col++; pwData++;
                        }
                        if (!empty)
                            (runs[runs.Count - 1] as RunItem).EndCol = lastCol;
                    }
                }
                runs.TrimToSize();
                nRuns = runs.Count;

                #endregion

                int[] labels = new int[nRuns];
                int nextLabel = 0;
                #region Process runs
                runQueue = new RunQueue(nDistance + 1);

                int firstRunOnThePreviousRow = 0,
                    lastRunOnThePreviousRow = 0;

                float fDistance2 = fDistance * fDistance;
                RunItem thisRun = null, pRun = null;
                int k = 0, p = 0, lastRow = -1, deltaRow = 0;
                int[] offset = new int[nDistance];
                for (int iDeltaRow = 0; iDeltaRow < nDistance; iDeltaRow++)
                    //offset[iDeltaRow] = (int)Math.Floor(Math.Sqrt(2 * fDistance * iDeltaRow - iDeltaRow * iDeltaRow)) + 1;
                    offset[iDeltaRow] = (int)Math.Floor(Math.Sqrt(fDistance2 - (iDeltaRow + 1) * (iDeltaRow + 1)));

                EquivItem testEquivItem = new EquivItem(-1, -1);
                for (k = 0; k < nRuns; k++)
                {
                    /* Process k-th run */
                    thisRun = (RunItem)runs[k];
                    deltaRow = thisRun.Row - lastRow;
                    if (deltaRow > nDistance) // new block
                    {
                        runQueue.Reset();
                        runQueue[nDistance].FirstRun = k;
                        lastRow = thisRun.Row;
                    }
                    else
                    {
                        if (deltaRow > 0) // new row, push into the queue
                        {
                            runQueue[nDistance].LastRun = k - 1;
                            runQueue.MoveIndexForwardAndReset(deltaRow);
                            runQueue[nDistance].FirstRun = k;
                            lastRow = thisRun.Row;
                        }
                        for (int iDeltaRow = 0; iDeltaRow < nDistance; iDeltaRow++)
                        {
                            firstRunOnThePreviousRow = runQueue[iDeltaRow].FirstRun;
                            lastRunOnThePreviousRow = runQueue[iDeltaRow].LastRun;

                            if (firstRunOnThePreviousRow >= 0) // Look for overlaps on previous rows
                            {
                                p = firstRunOnThePreviousRow;
                                pRun = (RunItem)runs[p];
                                while (p <= lastRunOnThePreviousRow)
                                {
                                    pRun = (RunItem)runs[p];
                                    int startP = pRun.StartCol - offset[nDistance - 1 - iDeltaRow];
                                    int endP = pRun.EndCol + offset[nDistance - 1 - iDeltaRow];

                                    if (startP > thisRun.EndCol)
                                        break;
                                    if ((thisRun.StartCol >= startP && thisRun.StartCol <= endP) ||
                                        (thisRun.EndCol >= startP && thisRun.EndCol <= endP) ||
                                        (startP >= thisRun.StartCol && startP <= thisRun.EndCol) ||
                                        (endP >= thisRun.StartCol && endP <= thisRun.EndCol))
                                    {
                                        //We've got an overlap 
                                        if (labels[k] == 0)//This run hasn't yet been labeled; copy over the overlapping run's label
                                        {
                                            labels[k] = labels[p];
                                        }
                                        else
                                        {
                                            if (labels[k] != labels[p]) // This run and the overlapping run have been labeled with different labels.  Remember the equivalence.
                                            {
                                                testEquivItem.K = labels[k];
                                                testEquivItem.P = labels[p];
                                                if (equivList.IndexOf(testEquivItem) < 0)
                                                    equivList.Add(testEquivItem.Clone());
                                            }
                                            else { }// This run and the overlapping run have been labeled with the same label; nothing to do here.
                                        }
                                    }
                                    p++;
                                }//while (p <= lastRunOnThePreviousRow)
                            }//if (firstRunOnThePreviousRow >= 0)
                        }//for (int iDeltaRow = 0;  iDeltaRow < nDistance;  iDeltaRow ++)
                    }

                    if (labels[k] == 0) // This run hasn't yet been labeled because we didn't find any overlapping runs.  Label it with a new label.
                    {
                        labels[k] = ++nextLabel;
                    }
                }
                equivList.TrimToSize();
                nClusters = nextLabel;

                #endregion

                #region Process EquivList
                int[] permut = new int[nextLabel + 1];
                for (int i = 0; i < nextLabel + 1; i++)
                    permut[i] = i;

                if (equivList.Count > 0)
                {
                    int search = 0, K = 0, P = 0, tmp = 0;
                    foreach (EquivItem item in equivList)
                    {
                        P = item.P;
                        K = item.K;
                        search = P;
                        do
                        {
                            search = permut[search];
                        }
                        while (search != P && search != K);
                        if (search == P)
                        {
                            tmp = permut[K];
                            permut[K] = permut[P];
                            permut[P] = tmp;
                        }
                    }

                    int c = 0, current = 0;
                    for (int i = 1; i <= nextLabel; i++)
                    {
                        if (i <= permut[i])
                        {
                            c++;
                            current = i;
                            while (permut[current] != i)
                            {
                                int next = permut[current];
                                permut[current] = c;
                                current = next;
                            }
                            permut[current] = c;
                        }
                    }
                    nClusters = c;
                }
                for (int i = 0; i < nRuns; i++)
                {
                    labels[i] = permut[labels[i]];
                }
                for (int i = 0; i < nPoints; i++)
                {
                    if (labelMatrix[i] == 0)
                        continue;
                    labelMatrix[i] = labels[labelMatrix[i] - 1];
                }
                #endregion

                return 0;
            }
            catch (System.Exception exp)
            {
                throw new System.ExecutionEngineException("Get clusters by CC", exp);
            }
            finally
            {
                #region Finalize
                if (runs != null)
                {
                    runs.Clear();
                    runs = null;
                }
                if (countDefRun != null)
                {
                    countDefRun.Clear();
                    countDefRun = null;
                }
                if (equivList != null)
                {
                    equivList.Clear();
                    equivList = null;
                }
                if (runQueue != null)
                {
                    runQueue.Dispose();
                    runQueue = null;
                }
                #endregion Finalize
            }
        }
        #endregion
    }
}
