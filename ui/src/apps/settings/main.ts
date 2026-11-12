import { mount } from 'svelte';
import '$lib/styles/theme.css';
import App from './App.svelte';

const target = document.getElementById('app');
if (!target) throw new Error('Chybí #app');

export default mount(App, { target });
