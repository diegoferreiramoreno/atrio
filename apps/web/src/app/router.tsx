import { createBrowserRouter } from 'react-router-dom';
import { StartPage } from './pages/StartPage';
import { NotFoundPage } from './pages/NotFoundPage';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <StartPage />,
  },
  {
    path: '*',
    element: <NotFoundPage />,
  },
]);
